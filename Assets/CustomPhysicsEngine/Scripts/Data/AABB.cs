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
    }
}

