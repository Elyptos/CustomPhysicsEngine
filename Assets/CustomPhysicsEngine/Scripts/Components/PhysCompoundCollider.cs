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
        public bool IsTrigger = false;

        [HideInInspector]
        public PhysCollider[] Collider;

        [SerializeField]
        private bool showBoundsInGame;

        public Vector2 MassPoint { get; set; }

        public int CollisionLayer { get; set; }

        public float InvMass { get; protected set; }
        public float InvInertia { get; protected set; }

        protected HashSet<PhysCompoundCollider> collisionSet = new HashSet<PhysCompoundCollider>();

        public bool IsValid { get; set; }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
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
            CollisionLayer = gameObject.layer;
        }


        public virtual void PhysicsUpdate(float deltaTime)
        {

        }

        public virtual void PrePhysicsUpdate()
        {

        }

        public virtual void PostPhysicsUpdate()
        {

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

        protected virtual void FixedUpdate()
        {
            //TODO: May result in performance problems. But we have to iterate through a copy in order to withstand a possible deletion of a collider component.
            HashSet<PhysCompoundCollider> copy = new HashSet<PhysCompoundCollider>(collisionSet);

            foreach(PhysCompoundCollider other in copy)
            {
                FCollision coll = new FCollision()
                {
                    Other = other.gameObject,
                    OtherCollider = other,
                    OtherRigidbody = other as PhysRigidbody
                };

                if(IsTrigger || other.IsTrigger)
                {
                    PhysicsEngine.EventManager.InvokeTriggerStay(this.gameObject, coll);
                }
                else
                {
                    PhysicsEngine.EventManager.InvokeCollisionStay(this.gameObject, coll);
                }
            }
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

            IsValid = true;
        }

        protected override void OnUnregister()
        {
            IsValid = false;
            ExitFromAllCollisions();

            PhysicsEngine.UnregisterCollider(this);
        }

        public void RemoteRegisterCollision(PhysCompoundCollider coll)
        {
            if(coll.IsValid)
                collisionSet.Add(coll);
        }

        public void RemoteUnregisterCollision(PhysCompoundCollider coll)
        {
            collisionSet.Remove(coll);
        }

        protected void ExitFromAllCollisions()
        {
            FCollision coll = new FCollision();
            coll.Other = this.gameObject;
            coll.OtherCollider = this;
            coll.OtherRigidbody = null;

            FCollision other = new FCollision();

            foreach (var elem in collisionSet)
            {
                elem.RemoteUnregisterCollision(this);

                PhysicsEngine.EventManager.InvokeCollisionExit(elem.gameObject, coll);

                other.Other = elem.gameObject;
                other.OtherCollider = elem;
                other.OtherRigidbody = elem as PhysRigidbody;

                PhysicsEngine.EventManager.InvokeCollisionExit(this.gameObject, other);
            }

            collisionSet.Clear();
        }
    }
}

