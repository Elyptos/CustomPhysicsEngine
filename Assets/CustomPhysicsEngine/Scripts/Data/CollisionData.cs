using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public struct FContact
    {
        public Vector2 Position;
        public Vector2 Normal;
        public float Penetration;
    }

    public class Manifold
    {
        public static readonly int MAX_CONTACTS = 2;

        public FContact[] Contacts;
        public PhysRigidbody A;
        public PhysRigidbody B;
        public Vector2 EdgeNormalA;
        public Vector2 EdgeNormalB;
        public PhysCompoundCollider BComp;
        public bool BodyAInc;
    }
}

