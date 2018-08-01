using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Phys
{
    public class PhysBoxCollider2D : PhysCollider
    {
        public FCollRect CollisionRect = new FCollRect() { Size = Vector2.one };

        private Manifold manifold;

        public FCollPoly CollisionBody
        {
            get;
            private set;
        }

        protected override Color ColliderColor
        {
            get { return Color.blue; }
        }

#if UNITY_EDITOR
        protected override void DrawCollider()
        {
            base.DrawCollider();

            Color fillColor = ColliderColor;

            fillColor.a = FILL_COLOR_ALPHA;

            UpdateCollisionBody_internal();

            Handles.DrawSolidRectangleWithOutline(new Vector3[] {
                CollisionBody.Vertices[0],
                CollisionBody.Vertices[1],
                CollisionBody.Vertices[2],
                CollisionBody.Vertices[3]
            }, fillColor, ColliderColor);

            if (manifold != null)
            {
                Handles.color = Color.red;

                foreach(var elem in manifold.Contacts)
                {
                    Handles.DrawSolidDisc(elem.Position, Vector3.forward, 0.1f);
                }
            }
        }
#endif

        protected override void UpdateCollisionBody_internal()
        {
            CollisionRect.TRS = FMatrix3x3.CreateTRS(transform.position, transform.rotation.eulerAngles.z, transform.localScale);

            CollisionBody = CollisionRect.ToPoly();
        }

        public override bool IsColliding(PhysCollider collider)
        {
            if(collider is PhysBoxCollider2D)
            {
                PhysBoxCollider2D other = collider as PhysBoxCollider2D;

                return CollisionDetector.IsCollidingRect(CollisionBody, other.CollisionBody, out manifold);
            }
            //else if(collider is PhysSphereCollider2D)
            //{
            //    PhysSphereCollider2D other = collider as PhysSphereCollider2D;

            //    return CollisionDetector.IsCollidingRect(CollisionBody, other.CollisionBody);
            //}

            return false;
        }
    }
}

