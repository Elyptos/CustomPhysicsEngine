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

        public float InvMass { get; protected set; }
        public float InvInertia { get; protected set; }

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
                CachedBoundsWS.Min,
                new Vector2(CachedBoundsWS.Min.x, CachedBoundsWS.Max.y),

                new Vector2(CachedBoundsWS.Min.x, CachedBoundsWS.Max.y),
                CachedBoundsWS.Max,

                CachedBoundsWS.Max,
                new Vector2(CachedBoundsWS.Max.x, CachedBoundsWS.Min.y),

                new Vector2(CachedBoundsWS.Max.x, CachedBoundsWS.Min.y),
                CachedBoundsWS.Min
            }, 1.0f);
        }
#endif

        public virtual void Warmup()
        {
            MassPoint = transform.position;
        }

        public virtual void UpdateAllCollider()
        {
            Area = 0f;
            Mass = 0f;
            Inertia = 0f;

            for (int i = 0; i < Collider.Length; i++)
            {
                if(Collider[i].NeedsBodyUpdate())
                    Collider[i].UpdateCollisionBody(0f);

                Collider[i].CalculateWSCollisionBody();

                Area += Collider[i].Area;
                Inertia += Collider[i].Inertia;
            }

            InvMass = Mass == 0f ? 0f : 1f / Mass;
            InvInertia = Inertia == 0f ? 0f : 1f / Inertia;
        }

        protected override FAABB2D EvaluateBounds_internal()
        {
            if (Collider.Length == 0)
                return new FAABB2D();

            Collider[0].EvaluateBounds();

            FAABB2D res = Collider[0].CachedBounds;

            for (int i = 1; i < Collider.Length; i++)
            {
                Collider[i].EvaluateBounds();

                res = FAABB2D.Combine(res, Collider[i].CachedBounds);
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

