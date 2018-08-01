using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public struct FMatrix3x3
    {
        public float m00;
        public float m01;
        public float m02;
        public float m10;
        public float m11;
        public float m12;
        public float m20;
        public float m21;
        public float m22;

        public static FMatrix3x3 identity
        {
            get
            {
                FMatrix3x3 matrix = new FMatrix3x3();
                matrix.m00 = 1f;
                matrix.m11 = 1f;
                matrix.m22 = 1f;

                return matrix;
            }
        }

        public float Determinant
        {
            get
            {
                return m00 * (m11 * m22 - m12 * m21)
                - m01 * (m10 * m22 - m12 * m20)
                + m02 * (m10 * m21 - m11 * m20);
            }
        }

        public static Vector2 MultiplyVector2(FMatrix3x3 m1, Vector2 vec)
        {
            Vector2 outVector = new Vector2();
            outVector.x = m1.m00 * vec.x + m1.m01 * vec.y + m1.m02;
            outVector.y = m1.m10 * vec.x + m1.m11 * vec.y + m1.m12;
            return outVector;
        }

        public static Vector2 operator *(FMatrix3x3 m, Vector2 v)
        {
            return FMatrix3x3.MultiplyVector2(m, v);
        }

        public static FMatrix3x3 CreateTRS(Vector2 translation, float rotation, Vector2 scale)
        {
            float cos = Mathf.Cos(rotation * Mathf.Deg2Rad);
            float sin = Mathf.Sin(rotation * Mathf.Deg2Rad);

            FMatrix3x3 m = new FMatrix3x3();
            m.m00 = scale.x * cos;
            m.m01 = scale.y * -sin;
            m.m02 = translation.x;
            m.m10 = scale.x * sin;
            m.m11 = scale.y * cos;
            m.m12 = translation.y;
            m.m20 = 0;
            m.m21 = 0;
            m.m22 = 1;
            return m;
        }
    }
}

