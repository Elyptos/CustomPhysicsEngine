using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public struct FCollPoly
    {
        public Vector2[] Vertices;

        public Vector2 Center
        {
            get
            {
                Vector2 res = Vector2.zero;

                for (int i = 0; i < Vertices.Length; i++)
                {
                    res += Vertices[i];
                }

                return res / Vertices.Length;
            }
        }
    }

    [Serializable]
    public struct FCollSphere
    {
        public Vector2 Center;
        public float Radius;
    }

    [Serializable]
    public struct FCollRect
    {
        public Vector2 Size;
        public Vector2 Offset;
        public FMatrix3x3 TRS;

        public FCollPoly ToPoly()
        {
            Vector2 halfSize = Size * 0.5f;

            Vector2[] vertices = new Vector2[4]
            {
                TRS * (new Vector2(-halfSize.x, -halfSize.y) + Offset),
                TRS * (new Vector2(halfSize.x, -halfSize.y)  + Offset),
                TRS * (new Vector2(halfSize.x, halfSize.y)  + Offset),
                TRS * (new Vector2(-halfSize.x, halfSize.y)  + Offset)
            };

            return new FCollPoly()
            {
                Vertices = vertices
            };
        }
    }
}

