using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    [AddComponentMenu("")]
    public class PhysComponent : MonoBehaviour
    {
        private int gObjectID = 0;

        public FAABB2D CachedBounds
        {
            get;
            private set;
        }

        private void OnEnable()
        {
            gObjectID = gameObject.GetInstanceID();

            if (gameObject.activeInHierarchy)
                OnRegister();
        }

        private void OnDisable()
        {
            if (gameObject.activeInHierarchy)
                OnUnregister();
        }

        public virtual void PhysicsUpdate(float deltaTime)
        {

        }

        public virtual void PostPhysicsUpdate()
        {

        }

        protected virtual void OnRegister()
        {

        }

        protected virtual void OnUnregister()
        {

        }

        public override int GetHashCode()
        {
            return gObjectID.GetHashCode();
        }

        public override bool Equals(object other)
        {
            return base.Equals(other);
        }

        public FAABB2D EvaluateBounds()
        {
            CachedBounds = EvaluateBounds_internal();
            return CachedBounds;
        }

        protected virtual FAABB2D EvaluateBounds_internal()
        {
            return new FAABB2D();
        }
    }
}

