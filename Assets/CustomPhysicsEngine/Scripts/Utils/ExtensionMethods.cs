using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public static class Vector2Extensions
    {
        public static Vector2 Multiply(this Vector2 vec, Vector2 other)
        {
            return new Vector2(vec.x * other.x, vec.y * other.y);
        }

        public static bool DeltaEquals(this Vector2 vec, Vector2 other, float delta)
        {
            return  vec.x <= (other.x + delta) && vec.x >= (other.x - delta) &&
                    vec.y <= (other.y + delta) && vec.y >= (other.y - delta);
        }

        public static float Cross(this Vector2 vec, Vector2 other)
        {
            return vec.x * other.y - vec.y * other.x;
        }

        public static Vector2 Cross(this Vector2 vec, float s)
        {
            return new Vector2(-s * vec.y, s * vec.x);
        }
    }

    public static class DatatypeExtensions
    {
        public static bool DeltaEquals(this float f, float other, float delta)
        {
            return f <= (other + delta) && f >= (other - delta);
        }
    }

}
