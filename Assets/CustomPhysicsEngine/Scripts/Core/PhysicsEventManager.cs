using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public struct FCollision
    {
        public GameObject Other;
        public PhysCompoundCollider OtherCollider;
        public PhysRigidbody OtherRigidbody;
    }

    public class PhysicsEventManager
    {
        private Dictionary<GameObject, Action<FCollision>> onCollisionEnter = new Dictionary<GameObject, Action<FCollision>>();
        private Dictionary<GameObject, Action<FCollision>> onCollisionExit = new Dictionary<GameObject, Action<FCollision>>();
        private Dictionary<GameObject, Action<FCollision>> onCollisionStay = new Dictionary<GameObject, Action<FCollision>>();
        private Dictionary<GameObject, Action<FCollision>> onTriggerEnter = new Dictionary<GameObject, Action<FCollision>>();
        private Dictionary<GameObject, Action<FCollision>> onTriggerExit = new Dictionary<GameObject, Action<FCollision>>();
        private Dictionary<GameObject, Action<FCollision>> onTriggerStay = new Dictionary<GameObject, Action<FCollision>>();

        public void RegisterCollisionEnter(GameObject obj, Action<FCollision> method)
        {
            if(onCollisionEnter.ContainsKey(obj))
            {
                onCollisionEnter[obj] += method;
            }
            else
            {
                onCollisionEnter.Add(obj, method);
            }
        }

        public void RegisterCollisionExit(GameObject obj, Action<FCollision> method)
        {
            if (onCollisionExit.ContainsKey(obj))
            {
                onCollisionExit[obj] += method;
            }
            else
            {
                onCollisionExit.Add(obj, method);
            }
        }

        public void RegisterCollisionStay(GameObject obj, Action<FCollision> method)
        {
            if (onCollisionStay.ContainsKey(obj))
            {
                onCollisionStay[obj] += method;
            }
            else
            {
                onCollisionStay.Add(obj, method);
            }
        }

        public void RegisterTriggerEnter(GameObject obj, Action<FCollision> method)
        {
            if (onTriggerEnter.ContainsKey(obj))
            {
                onTriggerEnter[obj] += method;
            }
            else
            {
                onTriggerEnter.Add(obj, method);
            }
        }

        public void RegisterTriggerExit(GameObject obj, Action<FCollision> method)
        {
            if (onTriggerExit.ContainsKey(obj))
            {
                onTriggerExit[obj] += method;
            }
            else
            {
                onTriggerExit.Add(obj, method);
            }
        }

        public void RegisterTriggerStay(GameObject obj, Action<FCollision> method)
        {
            if (onTriggerStay.ContainsKey(obj))
            {
                onTriggerStay[obj] += method;
            }
            else
            {
                onTriggerStay.Add(obj, method);
            }
        }




        public void UnregisterCollisionEnter(GameObject obj, Action<FCollision> method)
        {
            if (onCollisionEnter.ContainsKey(obj))
            {
                onCollisionEnter[obj] -= method;

                if (onCollisionEnter[obj] == null)
                    onCollisionEnter.Remove(obj);
            }
        }

        public void UnregisterCollisionExit(GameObject obj, Action<FCollision> method)
        {
            if (onCollisionExit.ContainsKey(obj))
            {
                onCollisionExit[obj] -= method;

                if (onCollisionExit[obj] == null)
                    onCollisionExit.Remove(obj);
            }
        }

        public void UnregisterCollisionStay(GameObject obj, Action<FCollision> method)
        {
            if (onCollisionStay.ContainsKey(obj))
            {
                onCollisionStay[obj] -= method;

                if (onCollisionStay[obj] == null)
                    onCollisionStay.Remove(obj);
            }
        }

        public void UnregisterTriggerEnter(GameObject obj, Action<FCollision> method)
        {
            if (onTriggerEnter.ContainsKey(obj))
            {
                onTriggerEnter[obj] -= method;

                if (onTriggerEnter[obj] == null)
                    onTriggerEnter.Remove(obj);
            }
        }

        public void UnregisterTriggerExit(GameObject obj, Action<FCollision> method)
        {
            if (onTriggerExit.ContainsKey(obj))
            {
                onTriggerExit[obj] -= method;

                if (onTriggerExit[obj] == null)
                    onTriggerExit.Remove(obj);
            }
        }

        public void UnregisterTriggerStay(GameObject obj, Action<FCollision> method)
        {
            if (onTriggerStay.ContainsKey(obj))
            {
                onTriggerStay[obj] -= method;

                if (onTriggerStay[obj] == null)
                    onTriggerStay.Remove(obj);
            }
        }




        public void InvokeCollisionEnter(GameObject obj, FCollision collision)
        {
            if(onCollisionEnter.ContainsKey(obj))
            {
                onCollisionEnter[obj]?.Invoke(collision);
            }
        }

        public void InvokeTriggerEnter(GameObject obj, FCollision collision)
        {
            if (onTriggerEnter.ContainsKey(obj))
            {
                onTriggerEnter[obj]?.Invoke(collision);
            }
        }

        public void InvokeCollisionExit(GameObject obj, FCollision collision)
        {
            if (onCollisionExit.ContainsKey(obj))
            {
                onCollisionExit[obj]?.Invoke(collision);
            }
        }

        public void InvokeTriggerExit(GameObject obj, FCollision collision)
        {
            if (onTriggerExit.ContainsKey(obj))
            {
                onTriggerExit[obj]?.Invoke(collision);
            }
        }

        public void InvokeCollisionStay(GameObject obj, FCollision collision)
        {
            if (onCollisionStay.ContainsKey(obj))
            {
                onCollisionStay[obj]?.Invoke(collision);
            }
        }

        public void InvokeTriggerStay(GameObject obj, FCollision collision)
        {
            if (onTriggerStay.ContainsKey(obj))
            {
                onTriggerStay[obj]?.Invoke(collision);
            }
        }
    }
}
