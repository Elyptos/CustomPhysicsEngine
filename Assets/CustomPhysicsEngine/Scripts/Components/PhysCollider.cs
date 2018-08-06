using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Phys
{
    public class PhysCollider : PhysComponent
    {
        protected const float BOUNDS_DASH_SIZE = 3.0f;
        protected const float FILL_COLOR_ALPHA = 0.1f;

        public bool showBounds;

        public bool IsDirty
        {
            get;
            private set;
        }

        protected virtual Color BoundsColor
        {
            get { return new Color(1f, 0.19f, 0.19f); }
        }

        protected virtual Color ColliderColor
        {
            get { return Color.black; }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Color handleColor = Handles.color;

            if (showBounds)
            {
                Handles.color = BoundsColor;
                DrawBounds();
            }

            Handles.color = ColliderColor;

            DrawCollider();

            Handles.color = handleColor;
        }

        private void DrawBounds()
        {
            FAABB2D bounds = EvaluateBounds();

            Handles.DrawDottedLines(new Vector3[]
            {
                bounds.Min,
                new Vector2(bounds.Min.x, bounds.Max.y),

                new Vector2(bounds.Min.x, bounds.Max.y),
                bounds.Max,

                bounds.Max,
                new Vector2(bounds.Max.x, bounds.Min.y),

                new Vector2(bounds.Max.x, bounds.Min.y),
                bounds.Min
            }, BOUNDS_DASH_SIZE);
        }

        protected virtual void DrawCollider()
        {

        }
#endif

        public void UpdateCollisionBody()
        {
            if (IsDirty)
                UpdateCollisionBody_internal();

            IsDirty = false;
        }

        protected virtual void UpdateCollisionBody_internal()
        {

        }

        public virtual bool IsColliding(PhysCollider collider, out Manifold manifold)
        {
            manifold = null;
            return false;
        }

        protected override void OnRegister()
        {
            IsDirty = true;

            if (GetComponent<PhysCompoundCollider>() == null)
                gameObject.AddComponent<PhysCompoundCollider>();
        }

        protected virtual void FixedUpdate()
        {
            IsDirty = true;
        }
    }
}

