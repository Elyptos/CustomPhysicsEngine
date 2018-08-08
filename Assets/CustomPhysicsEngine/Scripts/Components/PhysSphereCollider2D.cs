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
        [SerializeField]
        private float _radius = 0.5f;

        public float Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                IsDirty = true;
            }
        }

        public FCollSphere CollisionBodyWS
        {
            get;
            private set;
        }

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
            CalculateWSCollisionBody();

            Handles.DrawWireDisc(CollisionBodyWS.Center, Vector3.forward, CollisionBodyWS.Radius);

            Handles.color = fillColor;
            Handles.DrawSolidDisc(CollisionBodyWS.Center, Vector3.forward, CollisionBodyWS.Radius);

            //if (manifold != null && manifold.Contacts != null)
            //{
            //    Handles.color = Color.red;

            //    foreach (var elem in manifold.Contacts)
            //    {
            //        Handles.DrawSolidDisc(elem.Position, Vector3.forward, 0.1f);
            //    }
            //}
        }
#endif

        protected override FAABB2D EvaluateBounds_internal()
        {
            float radMod = Mathf.Max(transform.localScale.x, transform.localScale.y);
            
            Vector2 center = Offset;

            needsBoundsUpdate = false;

            return new FAABB2D()
            {
                Min = center - Radius * radMod * Vector2.one,
                Max = center + Radius * radMod * Vector2.one
            };
        }

        protected override void FixedUpdate()
        {
            if (oldScale != transform.localScale)
            {
                IsDirty = true;
            }

            oldScale = transform.localScale;
        }

        public override void CalculateWSCollisionBody()
        {
            CollisionBodyWS = new FCollSphere() { Radius = CollisionBody.Radius, Center = CollisionBody.Center + (Vector2)transform.position};
        }

        protected override void UpdateCollisionBody_internal()
        {
            float radMod = Mathf.Max(transform.localScale.x, transform.localScale.y);

            CollisionBody = new FCollSphere() { Center = Offset, Radius = Radius * radMod};
        }

        protected override void UpdateBodyInformation_internal(float density)
        {
            Area = Mathf.PI * Radius * Radius;
            Mass = Area * density;
            Inertia = 0.5f * Mass * Radius * Radius;
        }

        public override bool IsColliding(PhysCollider collider, out CollisionContact manifold, out bool isBodyA)
        {
            if (collider is PhysBoxCollider2D)
            {
                PhysBoxCollider2D other = collider as PhysBoxCollider2D;

                isBodyA = false;
                return CollisionDetector.IsCollidingRect(other.CollisionBodyWS, CollisionBodyWS, out manifold);
            }
            else if(collider is PhysSphereCollider2D)
            {
                PhysSphereCollider2D other = collider as PhysSphereCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingSphere(CollisionBodyWS, other.CollisionBodyWS, out manifold);
            }
            else if (collider is PhysPolyCollider2D)
            {
                PhysPolyCollider2D other = collider as PhysPolyCollider2D;

                isBodyA = false;
                return CollisionDetector.IsCollidingPoly(other.CollisionBodyWS, CollisionBodyWS, out manifold);
            }

            isBodyA = false;
            manifold = null;
            return false;
        }
    }
}

