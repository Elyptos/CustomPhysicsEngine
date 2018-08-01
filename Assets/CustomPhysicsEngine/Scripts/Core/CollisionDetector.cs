using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class CollisionDetector
    {

        private struct FSATAxis
        {
            public Vector2 Axis;
            public bool BodyA;
            public FEdge Edge;
            public float Penetration;
        }

        public static bool IsCollidingSphere(FCollSphere a, FCollSphere b, out Manifold manifold)
        {
            Vector2 rel = b.Center - a.Center;

            float dist = rel.sqrMagnitude;
            float rSum = a.Radius + b.Radius;

            if(dist > (rSum * rSum))
            {
                manifold = null;
                return false;
            }

            float delta = rel.magnitude;

            FContact contact = new FContact();

            if(delta == 0.0f)
            {
                contact.Penetration = a.Radius;
                contact.Normal = Vector2.up;
                contact.Position = a.Center;
            }
            else
            {
                contact.Penetration = rSum - delta;
                contact.Normal = rel / delta;
                contact.Position = contact.Normal * a.Radius + a.Center;
            }

            manifold = new Manifold();
            manifold.Contacts = new FContact[1];
            manifold.Contacts[0] = contact;

            return dist <= rSum;
        }

        public static bool IsCollidingRect(FCollPoly a, FCollPoly b, out Manifold manifold)
        {
            return RectSAT(a, b, out manifold);
        }

        public static bool IsCollidingRect(FCollPoly a, FCollSphere b)
        {
            return RectSAT(a, b);
        }

        private static bool RectSAT(FCollPoly p1, FCollSphere p2)
        {
            Vector2 a1 = (p1.Vertices[3] - p1.Vertices[0]).normalized;
            Vector2 a2 = (p1.Vertices[1] - p1.Vertices[0]).normalized;

            Vector2 a3 = (p2.Center - p1.Center).normalized;

            Vector2[] optV = new Vector2[2] { p1.Vertices[0], p1.Vertices[3] };

            if (!sat_IsCollidingOnAxis(ref optV, ref p2.Center, ref p2.Radius, a1))
                return false;

            optV[1] = p1.Vertices[1];

            if (!sat_IsCollidingOnAxis(ref optV, ref p2.Center, ref p2.Radius, a2))
                return false;

            if (!sat_IsCollidingOnAxis(ref p1.Vertices, ref p2.Center, ref p2.Radius, a3))
                return false;

            return true;
        }

        private static bool RectSAT(FCollPoly p1, FCollPoly p2, out Manifold manifold)
        {
            manifold = null;

            FSATAxis[] axis = new FSATAxis[4];

            axis[0].Axis = (p1.Vertices[0] - p1.Vertices[3]).normalized;
            axis[0].Edge = new FEdge(p1.Vertices[0], p1.Vertices[1], 0, 1);
            axis[0].BodyA = true;
            axis[1].Axis = (p1.Vertices[0] - p1.Vertices[1]).normalized;
            axis[1].Edge = new FEdge(p1.Vertices[3], p1.Vertices[0], 3, 0);
            axis[1].BodyA = true;
            axis[2].Axis = (p2.Vertices[0] - p2.Vertices[3]).normalized;
            axis[2].Edge = new FEdge(p2.Vertices[0], p2.Vertices[1], 0, 1);
            axis[2].BodyA = false;
            axis[3].Axis = (p2.Vertices[0] - p2.Vertices[1]).normalized;
            axis[3].Edge = new FEdge(p2.Vertices[3], p2.Vertices[0], 3, 0);
            axis[3].BodyA = false;

            Vector2[] optV = new Vector2[2] { p1.Vertices[0], p1.Vertices[3] };

            if (!sat_IsCollidingOnAxis(ref optV, ref p2.Vertices, axis[0].Axis, out axis[0].Penetration))
                return false;

            optV[1] = p1.Vertices[1];

            if (!sat_IsCollidingOnAxis(ref optV, ref p2.Vertices, axis[1].Axis, out axis[1].Penetration))
                return false;

            optV[0] = p2.Vertices[0];
            optV[1] = p2.Vertices[3];

            if (!sat_IsCollidingOnAxis(ref optV, ref p1.Vertices, axis[2].Axis, out axis[2].Penetration))
                return false;

            optV[1] = p2.Vertices[1];

            if (!sat_IsCollidingOnAxis(ref optV, ref p1.Vertices, axis[3].Axis, out axis[3].Penetration))
                return false;

            manifold = new Manifold();
            manifold.Contacts = new FContact[Manifold.MAX_CONTACTS];

            float minMax = Mathf.Abs(axis[0].Penetration);
            int minPenIndex = 0;

            for(int i = 1; i < axis.Length; i++)
            {
                if(Mathf.Abs(axis[i].Penetration) < minMax)
                {
                    minMax = Mathf.Abs(axis[i].Penetration);
                    minPenIndex = i;
                }
            }

            FSATAxis intersectionAxis = axis[minPenIndex];

            FEdge eRef;
            FEdge eInc;

            if(intersectionAxis.BodyA)
            {
                eRef = GetBestEdge_sat(ref p1.Vertices, intersectionAxis.Axis);
                eInc = GetBestEdge_sat(ref p2.Vertices, -intersectionAxis.Axis);
            }
            else
            {
                eRef = GetBestEdge_sat(ref p1.Vertices, -intersectionAxis.Axis);
                eInc = GetBestEdge_sat(ref p2.Vertices, intersectionAxis.Axis);
            }

            if(Mathf.Abs(Vector2.Dot(eInc.BV - eInc.AV, intersectionAxis.Axis)) < Mathf.Abs(Vector2.Dot(eRef.BV - eRef.AV, intersectionAxis.Axis)))
            {
                FEdge copy = eRef;
                eRef = eInc;
                eInc = copy;
            }

            eRef.Normal = MeshUtils.CalcNormalCC(eRef.AV, eRef.BV);

            Vector2 refDir = (eRef.BV - eRef.AV).normalized;

            float offset = Vector2.Dot(refDir, eRef.AV);

            List<FContact> contactPoints = Clip_sat(eInc.AV, eInc.BV, refDir, offset);

            if(contactPoints.Count < 2)
                return true;

            offset = Vector2.Dot(eRef.BV, refDir);

            contactPoints = Clip_sat(contactPoints[0].Position, contactPoints[1].Position, -refDir, -offset);

            if (contactPoints.Count < 2)
                return true;

            manifold.Contacts = contactPoints.ToArray();

            return true;
        }

        private static List<FContact> Clip_sat(Vector2 v1, Vector2 v2, Vector2 normal, float offset)
        {
            List<FContact> contactPoints = new List<FContact>();

            float d1 = Vector2.Dot(normal, v1) - offset;
            float d2 = Vector2.Dot(normal, v2) - offset;

            if (d1 >= 0f)
                contactPoints.Add(new FContact() { Position = v1 });

            if (d2 >= 0f)
                contactPoints.Add(new FContact() { Position = v2 });

            if(d1 * d2 < 0f)
            {
                Vector2 edge = v2 - v1;

                float edgeOffset = d1 / (d1 - d2);

                edge *= edgeOffset;
                edge += v1;

                contactPoints.Add(new FContact() { Position = edge });
            }

            return contactPoints;
        }

        private static FEdge GetBestEdge_sat(ref Vector2[] vertices, Vector2 normal)
        {
            float maxDot = Vector2.Dot(vertices[0], normal);
            int vertIndex = 0;

            for(int i = 1; i < vertices.Length; i++)
            {
                float dot = Vector2.Dot(vertices[i], normal);

                if(dot > maxDot)
                {
                    maxDot = dot;
                    vertIndex = i;
                }
            }

            FEdge[] ngb = CollMeshUtils.GetVertexEdges(vertIndex, ref vertices);

            Vector2 e1Dir = ngb[0].BV - ngb[0].AV;
            Vector2 e2Dir = ngb[1].BV - ngb[1].AV;

            if(Vector2.Dot(normal, e1Dir) <= Vector2.Dot(normal, e2Dir))
            {
                return ngb[0];
            }
            else
            {
                return ngb[1];
            }
        }

        private static bool sat_IsCollidingOnAxis(ref Vector2[] objAVertices, ref Vector2 sphereCenter, ref float sphereRad, Vector2 axis)
        {
            float s = 0f;

            s = sat_CalcProjScalar(ref objAVertices[0], ref axis);

            Vector2 objAProj = new Vector2(s, s);

            for (int i = 1; i < objAVertices.Length; i++)
            {
                s = sat_CalcProjScalar(ref objAVertices[i], ref axis);

                objAProj.x = Mathf.Min(objAProj.x, s);
                objAProj.y = Mathf.Max(objAProj.y, s);
            }

            float dotSphere = sat_CalcProjScalar(ref sphereCenter, ref axis);

            return Mathf.Abs(objAProj.x - dotSphere) <= sphereRad || Mathf.Abs(objAProj.y - dotSphere) <= sphereRad;
        }

        private static bool sat_IsCollidingOnAxis(ref Vector2[] objAVertices, ref Vector2[] objBVertices, Vector2 axis, out float penetration)
        {
            float s = 0f;

            s = sat_CalcProjScalar(ref objAVertices[0], ref axis);

            Vector2 objAProj = new Vector2(s, s);

            for (int i = 1; i < objAVertices.Length; i++)
            {
                s = sat_CalcProjScalar(ref objAVertices[i], ref axis);

                objAProj.x = Mathf.Min(objAProj.x, s);
                objAProj.y = Mathf.Max(objAProj.y, s);
            }

            s = sat_CalcProjScalar(ref objBVertices[0], ref axis);

            Vector2 objBProj = new Vector2(s, s);

            for (int i = 1; i < objBVertices.Length; i++)
            {
                s = sat_CalcProjScalar(ref objBVertices[i], ref axis);

                objBProj.x = Mathf.Min(objBProj.x, s);
                objBProj.y = Mathf.Max(objBProj.y, s);
            }

            if(objAProj.x <= objBProj.y && objBProj.x <= objAProj.y)
            {
                penetration = objBProj.x - objAProj.y;

                return true;
            }
            else
            {
                penetration = 0f;
                return false;
            }
        }

        private static float sat_CalcProjScalar(ref Vector2 point, ref Vector2 axis)
        {
            return Vector2.Dot(point, axis);
        }
    }
}

