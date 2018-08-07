using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class CollisionContact
    {
        public Vector2[] ContactPoints;
        public PhysCompoundCollider A;
        public PhysCompoundCollider B;
        public Vector2 EdgeNormalA;
        public Vector2 EdgeNormalB;
        public bool BodyAInc;
        public Vector2 Normal;
        public float Penetration;
    }
}

