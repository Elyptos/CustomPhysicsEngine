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

    public struct FManifold
    {
        public static readonly int MAX_CONTACTS = 2;

        FContact[] Contacts;
        PhysCollider A;
        PhysCollider B;
    }
}

