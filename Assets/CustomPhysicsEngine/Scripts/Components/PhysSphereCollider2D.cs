using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Phys
{
    public class PhysSphereCollider2D : PhysCollider
    {
        public FCollSphere CollisionSphere = new FCollSphere() { Radius = 0.5f };

        public FCollSphere CollisionBody
        {
            get;
            private set;
        }

        protected override Color ColliderColor
        {
            get { return Color.green; }
        }

#if UNITY_EDITOR
        protected override void DrawCollider()
        {
            base.DrawCollider();

            Color fillColor = ColliderColor;

            fillColor.a = FILL_COLOR_ALPHA;

            UpdateCollisionBody_internal();

            Handles.DrawWireDisc(CollisionBody.Center, Vector3.forward, CollisionBody.Radius);

            Handles.color = fillColor;
            Handles.DrawSolidDisc(CollisionBody.Center, Vector3.forward, CollisionBody.Radius);
        }
#endif

        protected override void UpdateCollisionBody_internal()
        {
            float radMod = Mathf.Max(transform.localScale.x, transform.localScale.y);

            CollisionBody = new FCollSphere() { Center = CollisionSphere.Center + (Vector2)transform.position, Radius = CollisionSphere.Radius * radMod};
        }

        public override bool IsColliding(PhysCollider collider)
        {
            if (collider is PhysBoxCollider2D)
            {
                PhysBoxCollider2D other = collider as PhysBoxCollider2D;

                return CollisionDetector.IsCollidingRect(other.CollisionBody, CollisionBody);
            }
            else if(collider is PhysSphereCollider2D)
            {
                PhysSphereCollider2D other = collider as PhysSphereCollider2D;

                return CollisionDetector.IsCollidingSphere(CollisionBody, other.CollisionBody);
            }

            return false;
        }
    }
}

