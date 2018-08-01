using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class CollisionDebug : MonoBehaviour
    {
        public PhysBoxCollider2D col1;
        public PhysBoxCollider2D col2;


        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            //DebugBoundingBox();
        }

        private void DebugBoundingBox()
        {
            FAABB2D bounds1 = col1.EvaluateBounds();
            FAABB2D bounds2 = col2.EvaluateBounds();

            Debug.Log("BB Collision: " + bounds1.IsInside(bounds2));
        }
    }
}

