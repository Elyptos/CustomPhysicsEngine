using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class CollisionDetector
    {
        public static bool IsCollidingSphere(FCollSphere a, FCollSphere b)
        {
            float dist = (b.Center - a.Center).sqrMagnitude;
            float rSum = (a.Radius + b.Radius) * (a.Radius + b.Radius);

            return dist <= rSum;
        }

        public static bool IsCollidingRect(FCollPoly a, FCollPoly b)
        {
            return RectSAT(a, b);
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

        private static bool RectSAT(FCollPoly p1, FCollPoly p2)
        {
            Vector2 a1 = (p1.Vertices[3] - p1.Vertices[0]).normalized;
            Vector2 a2 = (p1.Vertices[1] - p1.Vertices[0]).normalized;
            Vector2 a3 = (p2.Vertices[3] - p2.Vertices[0]).normalized;
            Vector2 a4 = (p2.Vertices[1] - p2.Vertices[0]).normalized;

            Vector2[] optV = new Vector2[2] { p1.Vertices[0], p1.Vertices[3] };

            if (!sat_IsCollidingOnAxis(ref optV, ref p2.Vertices, a1))
                return false;

            optV[1] = p1.Vertices[1];

            if (!sat_IsCollidingOnAxis(ref optV, ref p2.Vertices, a2))
                return false;

            optV[0] = p2.Vertices[0];
            optV[1] = p2.Vertices[3];

            if (!sat_IsCollidingOnAxis(ref optV, ref p1.Vertices, a3))
                return false;

            optV[1] = p2.Vertices[1];

            if (!sat_IsCollidingOnAxis(ref optV, ref p1.Vertices, a4))
                return false;

            return true;
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

        private static bool sat_IsCollidingOnAxis(ref Vector2[] objAVertices, ref Vector2[] objBVertices, Vector2 axis)
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

            return objAProj.x <= objBProj.y && objBProj.x <= objAProj.y;
        }

        private static float sat_CalcProjScalar(ref Vector2 point, ref Vector2 axis)
        {
            return Vector2.Dot(point, axis);
        }
    }
}

