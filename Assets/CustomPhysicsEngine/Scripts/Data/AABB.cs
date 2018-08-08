using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public struct FAABB2D
    {
        private Vector2 _min;

        public Vector2 Min
        {
            get { return _min; }
            set
            {
                _min = value;
            }
        }

        private Vector2 _max;

        public Vector2 Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public Vector2 Size
        {
            get { return Max - Min; }
        }

        public Vector2 Center
        {
            get { return (Min + Max) * 0.5f; }
        }

        public float Area
        {
            get { return (Max.x - Min.x) * (Max.y - Min.y); }
        }

        public static FAABB2D Add(FAABB2D box, Vector2 vec)
        {
            return new FAABB2D()
            {
                Min = box.Min + vec,
                Max = box.Max + vec
            };
        }

        public static FAABB2D Multiply(FAABB2D box, Vector2 vec)
        {
            return new FAABB2D()
            {
                Min = new Vector2(box.Min.x * vec.x, box.Min.y * vec.y),
                Max = new Vector2(box.Max.x * vec.x, box.Max.y * vec.y)
            };
        }

        public static FAABB2D Multiply(FAABB2D box, FMatrix3x3 matrix)
        {
            return new FAABB2D()
            {
                Min = matrix * box.Min,
                Max = matrix * box.Max
            };
        }

        public bool Intersects(FAABB2D other)
        {
            return Min.x <= other.Max.x && other.Min.x <= Max.x
                && Min.y <= other.Max.y && other.Min.y <= Max.y;
        }

        public bool IsInside(FAABB2D other)
        {
            return Min.x >= other.Min.x && Min.y >= other.Min.y
                && Max.x <= other.Max.x && Max.y <= other.Max.y;
        }

        public static FAABB2D Combine(FAABB2D a, FAABB2D b)
        {
            Vector2 min = a.Min;
            Vector2 max = a.Max;

            min.x = Mathf.Min(a.Min.x, b.Min.x);
            min.y = Mathf.Min(a.Min.y, b.Min.y);
            max.x = Mathf.Max(a.Max.x, b.Max.x);
            max.y = Mathf.Max(a.Max.y, b.Max.y);

            return new FAABB2D()
            {
                Min = min,
                Max = max
            };
        }

        public static FAABB2D operator *(FAABB2D box, Vector2 vec)
        {
            return Multiply(box, vec);
        }

        public static FAABB2D operator *(FAABB2D box, FMatrix3x3 matrix)
        {
            return Multiply(box, matrix);
        }

        public static FAABB2D operator +(FAABB2D box, Vector2 vec)
        {
            return Add(box, vec);
        }

        public static FAABB2D operator +(FAABB2D box1, FAABB2D box2)
        {
            return Combine(box1, box2);
        }
    }
}

