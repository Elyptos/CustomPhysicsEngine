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

        //private Manifold manifold = null;

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

            //if (manifold != null && manifold.Contacts != null)
            //{
            //    Handles.color = Color.red;

            //    foreach(var elem in manifold.Contacts)
            //    {
            //        Handles.DrawSolidDisc(elem.Position, Vector3.forward, 0.1f);
            //    }
            //}
        }
#endif

        protected override FAABB2D EvaluateBounds_internal()
        {
            CollisionRect.TRS = FMatrix3x3.CreateTRS(transform.position, transform.rotation.eulerAngles.z, transform.localScale);

            CollisionBody = CollisionRect.ToPoly();

            Vector2 xAxis = Vector2.right;
            Vector2 yAxis = Vector2.up;

            Vector2 minMaxX = new Vector2();
            Vector2 minMaxY = new Vector2();

            minMaxX.x = Vector2.Dot(xAxis, CollisionBody.Vertices[0]);
            minMaxX.y = minMaxX.x;

            minMaxY.x = Vector2.Dot(yAxis, CollisionBody.Vertices[0]);
            minMaxY.y = minMaxY.x;

            for (int i = 0; i < CollisionBody.Vertices.Length; i++)
            {
                float d = Vector2.Dot(xAxis, CollisionBody.Vertices[i]);

                if (d < minMaxX.x)
                {
                    minMaxX.x = d;
                }
                else if (d > minMaxX.y)
                {
                    minMaxX.y = d;
                }

                d = Vector2.Dot(yAxis, CollisionBody.Vertices[i]);

                if (d < minMaxY.x)
                {
                    minMaxY.x = d;
                }
                else if (d > minMaxY.y)
                {
                    minMaxY.y = d;
                }
            }

            return new FAABB2D()
            {
                Min = new Vector2(minMaxX.x, minMaxY.x),
                Max = new Vector2(minMaxX.y, minMaxY.y)
            };
        }

        protected override void UpdateCollisionBody_internal()
        {
            CollisionRect.TRS = FMatrix3x3.CreateTRS(transform.position, transform.rotation.eulerAngles.z, transform.localScale);

            CollisionBody = CollisionRect.ToPoly();
        }

        public override bool IsColliding(PhysCollider collider, out Manifold manifold)
        {
            if(collider is PhysBoxCollider2D)
            {
                PhysBoxCollider2D other = collider as PhysBoxCollider2D;

                return CollisionDetector.IsCollidingRect(CollisionBody, other.CollisionBody, out manifold);
            }
            else if (collider is PhysSphereCollider2D)
            {
                PhysSphereCollider2D other = collider as PhysSphereCollider2D;

                return CollisionDetector.IsCollidingRect(CollisionBody, other.CollisionBody, out manifold);
            }

            manifold = null;
            return false;
        }
    }
}

