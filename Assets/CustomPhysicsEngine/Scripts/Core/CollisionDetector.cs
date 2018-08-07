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

        public static bool IsCollidingSphere(FCollSphere a, FCollSphere b, out CollisionContact manifold)
        {
            bool flipRef = b.Center.x < a.Center.x || b.Center.y < a.Center.y;

            Vector2 rel = flipRef ? a.Center - b.Center : b.Center - a.Center;

            float dist = rel.sqrMagnitude;
            float rSum = a.Radius + b.Radius;

            if (dist > (rSum * rSum))
            {
                manifold = null;
                return false;
            }

            float delta = rel.magnitude;

            manifold = new CollisionContact();

            if (delta == 0.0f)
            {
                manifold.Penetration = a.Radius;
                manifold.Normal = Vector2.up;
                manifold.ContactPoints = new Vector2[] { a.Center };
            }
            else
            {
                manifold.Penetration = rSum - delta;
                manifold.Normal = rel / delta;
                manifold.ContactPoints = new Vector2[] { flipRef ? manifold.Normal * b.Radius + b.Center : manifold.Normal * a.Radius + a.Center };
            }

            manifold.BodyAInc = flipRef;
            
            if(flipRef)
            {
                manifold.EdgeNormalA = -manifold.Normal;
                manifold.EdgeNormalB = manifold.Normal;
            }
            else
            {
                manifold.EdgeNormalA = manifold.Normal;
                manifold.EdgeNormalB = -manifold.Normal;
            }

            return true;
        }

        public static bool IsCollidingRect(FCollPoly a, FCollPoly b, out CollisionContact manifold)
        {
            return PolySAT(a, b, out manifold);
        }

        public static bool IsCollidingRect(FCollPoly a, FCollSphere b, out CollisionContact manifold)
        {
            return SphereSAT(a, b, out manifold);
        }

        public static bool IsCollidingPoly(FCollPoly a, FCollPoly b, out CollisionContact manifold)
        {
            return PolySAT(a, b, out manifold);
        }

        public static bool IsCollidingPoly(FCollPoly a, FCollSphere b, out CollisionContact manifold)
        {
            return SphereSAT(a, b, out manifold);
        }

        private static bool SphereSAT(FCollPoly p1, FCollSphere p2, out CollisionContact manifold)
        {
            FSATAxis[] axisPoly = GetAxis_sat(ref p1.Vertices);
            FSATAxis circleAxis = GetCircleAxis_sat(p2.Center, ref p1.Vertices);
            FSATAxis collisionAxis = axisPoly[0];
            float penetration = 100000f;

            manifold = null;

            for (int i = 0; i < axisPoly.Length; i++)
            {
                Vector2 proj1 = ProjOntoAxis_sat(axisPoly[i], ref p1.Vertices);
                float circleCenter = Vector2.Dot(p2.Center, axisPoly[i].Axis);
                Vector2 proj2 = new Vector2(circleCenter - p2.Radius, circleCenter + p2.Radius);

                float overlap = 0f;

                if (!GetOverlapFromProjection_sat(proj1, proj2, out axisPoly[i].BodyA, out overlap))
                {
                    return false;
                }
                else
                {
                    if (ContainsOtherProjection_sat(proj1, proj2) || ContainsOtherProjection_sat(proj2, proj1))
                    {
                        float mins = Mathf.Abs(proj1.x - proj2.x);
                        float maxs = Mathf.Abs(proj1.y - proj2.y);

                        if (mins < maxs)
                        {
                            overlap += mins;
                        }
                        else
                        {
                            overlap += maxs;
                        }
                    }

                    if (overlap < penetration)
                    {
                        collisionAxis = axisPoly[i];
                        penetration = overlap;
                    }
                }
            }


            //Circle axis
            {
                Vector2 proj1 = ProjOntoAxis_sat(circleAxis, ref p1.Vertices);
                float circleCenter = Vector2.Dot(p2.Center, circleAxis.Axis);
                Vector2 proj2 = new Vector2(circleCenter - p2.Radius, circleCenter + p2.Radius);

                float overlap = 0f;

                if (!GetOverlapFromProjection_sat(proj1, proj2, out circleAxis.BodyA, out overlap))
                {
                    return false;
                }
                else
                {
                    if (ContainsOtherProjection_sat(proj1, proj2) || ContainsOtherProjection_sat(proj2, proj1))
                    {
                        float mins = Mathf.Abs(proj1.x - proj2.x);
                        float maxs = Mathf.Abs(proj1.y - proj2.y);

                        if (mins < maxs)
                        {
                            overlap += mins;
                        }
                        else
                        {
                            overlap += maxs;
                        }
                    }

                    if (overlap < penetration)
                    {
                        collisionAxis = circleAxis;
                        penetration = overlap;
                    }
                }
            }

            //Debug.Log("Collision axis: " + collisionAxis.Axis);
            //Debug.Log("Penetration: " + penetration);

            manifold = new CollisionContact();

            FEdge eRef;

            if (!collisionAxis.BodyA)
                collisionAxis.Axis *= -1.0f;

            eRef = GetBestEdge_sat(ref p1.Vertices, collisionAxis.Axis);

            eRef.Normal = MeshUtils.CalcNormal(eRef.AV, eRef.BV);

            Vector2 vLeft = p1.Vertices[CollMeshUtils.LeftIndex(eRef.AI, p1.Vertices.Length)];
            Vector2 vRight = p1.Vertices[CollMeshUtils.RightIndex(eRef.BI, p1.Vertices.Length)];
            Vector2 normLeft = MeshUtils.CalcNormalRaw(vLeft, eRef.AV);
            Vector2 normRight = MeshUtils.CalcNormalRaw(eRef.BV, vRight);
            Vector2 leftRel = p2.Center - eRef.AV;
            Vector2 rightRel = p2.Center - eRef.BV;

            if (Vector2.Dot(normLeft, leftRel) > 0f && Vector2.Dot(eRef.Normal, leftRel) > 0f)
            {
                manifold.Penetration = penetration;
                manifold.Normal = collisionAxis.Axis;
                manifold.ContactPoints = new Vector2[] { p2.Center - leftRel.normalized * p2.Radius };
            }
            else if(Vector2.Dot(normRight, rightRel) > 0f && Vector2.Dot(eRef.Normal, rightRel) > 0f)
            {
                manifold.Penetration = penetration;
                manifold.Normal = collisionAxis.Axis;
                manifold.ContactPoints = new Vector2[] { p2.Center - rightRel.normalized * p2.Radius };
            }
            else
            {
                manifold.Penetration = penetration;
                manifold.Normal = collisionAxis.Axis;
                manifold.ContactPoints = new Vector2[] { p2.Center - eRef.Normal * p2.Radius };
            }

            manifold.BodyAInc = false;
            manifold.EdgeNormalA = eRef.Normal;
            manifold.EdgeNormalB = (p2.Center - manifold.ContactPoints[0]).normalized;

            return true;
        }

        private static bool PolySAT(FCollPoly p1, FCollPoly p2, out CollisionContact manifold)
        {
            manifold = null;

            FSATAxis[] axis1 = GetAxis_sat(ref p1.Vertices);
            FSATAxis[] axis2 = GetAxis_sat(ref p2.Vertices);

            FSATAxis collisionAxis = axis1[0];
            float penetration = 100000f;

            for (int i = 0; i < axis1.Length; i++)
            {
                Vector2 proj1 = ProjOntoAxis_sat(axis1[i], ref p1.Vertices);
                Vector2 proj2 = ProjOntoAxis_sat(axis1[i], ref p2.Vertices);

                float overlap = 0f;

                if (!GetOverlapFromProjection_sat(proj1, proj2, out axis1[i].BodyA, out overlap))
                {
                    return false;
                }
                else
                {
                    if (ContainsOtherProjection_sat(proj1, proj2) || ContainsOtherProjection_sat(proj2, proj1))
                    {
                        float mins = Mathf.Abs(proj1.x - proj2.x);
                        float maxs = Mathf.Abs(proj1.y - proj2.y);

                        if (mins < maxs)
                        {
                            overlap += mins;
                        }
                        else
                        {
                            overlap += maxs;
                        }
                    }

                    if (overlap < penetration)
                    {
                        collisionAxis = axis1[i];
                        penetration = overlap;
                    }
                }
            }

            for (int i = 0; i < axis2.Length; i++)
            {
                Vector2 proj1 = ProjOntoAxis_sat(axis2[i], ref p1.Vertices);
                Vector2 proj2 = ProjOntoAxis_sat(axis2[i], ref p2.Vertices);

                float overlap = 0f;

                if (!GetOverlapFromProjection_sat(proj1, proj2, out axis2[i].BodyA, out overlap))
                {
                    return false;
                }
                else
                {
                    if (ContainsOtherProjection_sat(proj1, proj2) || ContainsOtherProjection_sat(proj2, proj1))
                    {
                        float mins = Mathf.Abs(proj1.x - proj2.x);
                        float maxs = Mathf.Abs(proj1.y - proj2.y);

                        if (mins < maxs)
                        {
                            overlap += mins;
                        }
                        else
                        {
                            overlap += maxs;
                        }
                    }

                    if (overlap < penetration)
                    {
                        collisionAxis = axis2[i];
                        penetration = overlap;
                    }
                }
            }

            manifold = new CollisionContact();

            FEdge eRef;
            FEdge eInc;

            if (collisionAxis.BodyA)
            {
                eRef = GetBestEdge_sat(ref p1.Vertices, collisionAxis.Axis);
                eInc = GetBestEdge_sat(ref p2.Vertices, -collisionAxis.Axis);
            }
            else
            {
                eInc = GetBestEdge_sat(ref p1.Vertices, -collisionAxis.Axis);
                eRef = GetBestEdge_sat(ref p2.Vertices, collisionAxis.Axis);
            }

            bool flipped = false;

            if (Mathf.Abs(Vector2.Dot(eInc.BV - eInc.AV, collisionAxis.Axis)) < Mathf.Abs(Vector2.Dot(eRef.BV - eRef.AV, collisionAxis.Axis)))
            {
                FEdge copy = eRef;
                eRef = eInc;
                eInc = copy;

                flipped = true;
            }

            Vector2 refDir = (eRef.BV - eRef.AV).normalized;

            float offset = Vector2.Dot(refDir, eRef.AV);

            List<Vector2> contactPoints = Clip_sat(eInc.AV, eInc.BV, refDir, offset);

            if (contactPoints.Count < 2)
                return true;

            offset = Vector2.Dot(eRef.BV, refDir);

            contactPoints = Clip_sat(contactPoints[0], contactPoints[1], -refDir, -offset);

            if (contactPoints.Count < 2)
                return true;

            eRef.Normal = MeshUtils.CalcNormal(eRef.AV, eRef.BV);
            eInc.Normal = MeshUtils.CalcNormal(eInc.AV, eInc.BV);

            //Debug.Log(eRef.Normal);

            if(Vector2.Dot(eRef.Normal, contactPoints[1] - eRef.AV) > 0f)
                contactPoints.RemoveAt(1);

            if (Vector2.Dot(eRef.Normal, contactPoints[0] - eRef.AV) > 0f)
                contactPoints.RemoveAt(0);

            manifold.ContactPoints = contactPoints.ToArray();
            manifold.Normal = -collisionAxis.Axis;
            manifold.Penetration = penetration;
            manifold.BodyAInc = collisionAxis.BodyA;
            manifold.EdgeNormalA = manifold.BodyAInc ? eInc.Normal : eRef.Normal;
            manifold.EdgeNormalB = manifold.BodyAInc ? eRef.Normal : eInc.Normal;

            return true;
        }

        private static bool ContainsOtherProjection_sat(Vector2 proj1, Vector2 proj2)
        {
            return proj2.x >= proj1.x && proj1.y >= proj2.y;
        }

        private static bool GetOverlapFromProjection_sat(Vector2 proj1, Vector2 proj2, out bool proj1First, out float overlapAmount)
        {
            overlapAmount = 0f;
            proj1First = false;

            if (proj1.x <= proj2.y && proj2.x <= proj1.y)
            {
                proj1First = proj1.x <= proj2.x;

                if(proj1First)
                {
                    overlapAmount = proj1.y - proj2.x;
                }
                else
                {
                    overlapAmount = proj2.y - proj1.x;
                }

                return true;
            }
            else
            {
                return false;
            }

        }

        private static Vector2 ProjOntoAxis_sat(FSATAxis axis, ref Vector2[] vertices)
        {
            Vector2 minMax = Vector2.zero;

            minMax.x = Vector2.Dot(axis.Axis, vertices[0]);
            minMax.y = minMax.x;

            for (int i = 1; i < vertices.Length; i++)
            {
                float d = Vector2.Dot(axis.Axis, vertices[i]);

                if (d < minMax.x)
                {
                    minMax.x = d;
                }
                else if (d > minMax.y)
                {
                    minMax.y = d;
                }
            }

            return minMax;
        }

        private static FSATAxis GetCircleAxis_sat(Vector2 circleCenter, ref Vector2[] polyVertices)
        {
            FSATAxis bestAxis = new FSATAxis()
            {
                Axis = circleCenter - polyVertices[0]
            };
            float shortestDist = bestAxis.Axis.sqrMagnitude;

            for(int i = 1; i < polyVertices.Length; i++)
            {
                Vector2 rel = circleCenter - polyVertices[i];
                float dist = rel.sqrMagnitude;

                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    bestAxis.Axis = rel;
                }
            }

            bestAxis.Axis.Normalize();

            return bestAxis;
        }

        private static FSATAxis[] GetAxis_sat(ref Vector2[] vertices)
        {
            FSATAxis[] res = new FSATAxis[vertices.Length];

            for (int i = 0; i < res.Length; i++)
            {
                int nxtVertex = CollMeshUtils.RightIndex(i, vertices.Length);

                res[i] = new FSATAxis();
                res[i].Edge = new FEdge(vertices[i], vertices[nxtVertex], i, nxtVertex);
                res[i].Axis = MeshUtils.CalcNormal(res[i].Edge.AV, res[i].Edge.BV);
                res[i].Edge.Normal = res[i].Axis;
            }

            return res;
        }

        private static FSATAxis[] GetRectAxis_sat(ref Vector2[] vertices)
        {
            FSATAxis[] res = new FSATAxis[2];

            res[0] = new FSATAxis();
            res[0].Edge = new FEdge(vertices[0], vertices[1], 0, 1);
            res[0].Axis = MeshUtils.CalcNormal(res[0].Edge.AV, res[0].Edge.BV);
            res[0].Edge.Normal = res[0].Axis;

            res[1] = new FSATAxis();
            res[1].Edge = new FEdge(vertices[1], vertices[2], 1, 2);
            res[1].Axis = MeshUtils.CalcNormal(res[1].Edge.AV, res[1].Edge.BV);
            res[1].Edge.Normal = res[1].Axis;

            return res;
        }

        private static List<Vector2> Clip_sat(Vector2 v1, Vector2 v2, Vector2 normal, float offset)
        {
            List<Vector2> contactPoints = new List<Vector2>();

            float d1 = Vector2.Dot(normal, v1) - offset;
            float d2 = Vector2.Dot(normal, v2) - offset;

            if (d1 >= 0f)
                contactPoints.Add(v1);

            if (d2 >= 0f)
                contactPoints.Add(v2);

            if (d1 * d2 < 0f)
            {
                Vector2 edge = v2 - v1;

                float edgeOffset = d1 / (d1 - d2);

                edge *= edgeOffset;
                edge += v1;

                contactPoints.Add(edge);
            }

            return contactPoints;
        }

        private static FEdge GetBestEdge_sat(ref Vector2[] vertices, Vector2 normal)
        {
            float maxDot = Vector2.Dot(vertices[0], normal);
            int vertIndex = 0;

            for (int i = 1; i < vertices.Length; i++)
            {
                float dot = Vector2.Dot(vertices[i], normal);

                if (dot > maxDot)
                {
                    maxDot = dot;
                    vertIndex = i;
                }
            }

            FEdge[] ngb = CollMeshUtils.GetVertexEdges(vertIndex, ref vertices);

            Vector2 e1Dir = ngb[0].BV - ngb[0].AV;
            Vector2 e2Dir = ngb[1].AV - ngb[1].BV;

            if (Vector2.Dot(normal, e1Dir) <= Vector2.Dot(normal, e2Dir))
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

            if (objAProj.x <= objBProj.y && objBProj.x <= objAProj.y)
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

