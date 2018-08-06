using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Phys
{
    [DisallowMultipleComponent]
    public class PhysCompoundCollider : PhysComponent
    {
        public float Roughness = 0.5f;

        [HideInInspector]
        public PhysCollider[] Collider;

        [SerializeField]
        private bool showBoundsInGame;

        public Vector2 MassPoint { get; set; }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Color handleColor = Handles.color;

            if (showBoundsInGame)
            {
                Handles.color = Color.red;
                DrawBounds();
            }

            Handles.color = handleColor;
        }

        private void DrawBounds()
        {
            Handles.DrawDottedLines(new Vector3[]
            {
                CachedBounds.Min,
                new Vector2(CachedBounds.Min.x, CachedBounds.Max.y),

                new Vector2(CachedBounds.Min.x, CachedBounds.Max.y),
                CachedBounds.Max,

                CachedBounds.Max,
                new Vector2(CachedBounds.Max.x, CachedBounds.Min.y),

                new Vector2(CachedBounds.Max.x, CachedBounds.Min.y),
                CachedBounds.Min
            }, 1.0f);
        }
#endif

        public virtual void Warmup()
        {
            MassPoint = transform.position;
        }

        public void UpdateAllCollider()
        {
            for(int i = 0; i < Collider.Length; i++)
            {
                Collider[i].UpdateCollisionBody();
            }
        }

        protected override FAABB2D EvaluateBounds_internal()
        {
            if (Collider.Length == 0)
                return new FAABB2D();

            FAABB2D res = Collider[0].EvaluateBounds();

            for (int i = 1; i < Collider.Length; i++)
            {
                res = FAABB2D.Combine(res, Collider[i].EvaluateBounds());
            }

            return res;
        }

        protected virtual void Awake()
        {
            Collider = GetComponents<PhysCollider>();
        }

        protected override void OnRegister()
        {
            PhysicsEngine.RegisterCollider(this);
        }

        protected override void OnUnregister()
        {
            PhysicsEngine.UnregisterCollider(this);
        }
    }
}

