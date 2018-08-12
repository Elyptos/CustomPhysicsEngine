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
        public bool showCollider = true;

        [SerializeField]
        private Vector2 _offset;

        public Vector2 Offset
        {
            get { return _offset; }
            set
            {
                _offset = value;
                IsDirty = true;
            }
        }

        protected float oldRotation;
        protected Vector3 oldScale;

        protected bool needsBoundsUpdate = false;

        public bool IsDirty
        {
            get;
            set;
        }

        protected virtual Color BoundsColor
        {
            get { return new Color(1f, 0.19f, 0.19f); }
        }

        protected virtual Color ColliderColor
        {
            get { return Color.black; }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color handleColor = Handles.color;

            if (showBounds)
            {
                Handles.color = BoundsColor;
                DrawBounds();
            }

            if(showCollider)
            {
                Handles.color = ColliderColor;

                DrawCollider();
            }

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

        public void UpdateCollisionBody(float density)
        {
            UpdateCollisionBody_internal();
            UpdateBodyInformation_internal(density);

            needsBoundsUpdate = true;
            IsDirty = false;
        }

        protected override bool AreCachedBoundsInvalid()
        {
            return needsBoundsUpdate;
        }

        public virtual void CalculateWSCollisionBody()
        {

        }

        protected virtual void UpdateCollisionBody_internal()
        {

        }

        protected virtual void UpdateBodyInformation_internal(float density)
        {

        }

        public virtual bool IsColliding(PhysCollider collider, out CollisionContact manifold, out bool isBodyA)
        {
            manifold = null;
            isBodyA = true;
            return false;
        }

        public virtual bool NeedsBodyUpdate()
        {
            return IsDirty;
        }

        protected override void OnRegister()
        {
            if (GetComponent<PhysCompoundCollider>() == null)
                gameObject.AddComponent<PhysCompoundCollider>();

            IsDirty = true;
        }

        protected virtual void FixedUpdate()
        {
            if (oldRotation != transform.rotation.eulerAngles.z || oldScale != transform.localScale)
            {
                IsDirty = true;
            }

            oldRotation = transform.rotation.eulerAngles.z;
            oldScale = transform.localScale;
        }
    }
}

