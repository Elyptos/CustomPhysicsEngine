using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class PhysComponent : MonoBehaviour
    {
        private void OnEnable()
        {
            OnRegister();
        }

        private void OnDisable()
        {
            OnUnregister();
        }

        protected virtual void OnRegister()
        {

        }

        protected virtual void OnUnregister()
        {

        }

        public override int GetHashCode()
        {
            return gameObject.GetInstanceID().GetHashCode();
        }

        public override bool Equals(object other)
        {
            return base.Equals(other);
        }
    }
}

