using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class EventDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            PhysicsEngine.EventManager.RegisterTriggerEnter(gameObject, CollisionEnter);
            PhysicsEngine.EventManager.RegisterTriggerExit(gameObject, CollisionExit);
            PhysicsEngine.EventManager.RegisterTriggerStay(gameObject, CollisionStay);
        }

        private void OnDisable()
        {
            PhysicsEngine.EventManager.UnregisterTriggerEnter(gameObject, CollisionEnter);
            PhysicsEngine.EventManager.UnregisterTriggerExit(gameObject, CollisionExit);
            PhysicsEngine.EventManager.UnregisterTriggerStay(gameObject, CollisionStay);
        }

        private void CollisionEnter(FCollision obj)
        {
            Debug.Log("Collision enter");
        }

        private void CollisionExit(FCollision obj)
        {
            Debug.Log("Collision exit");
        }

        private void CollisionStay(FCollision obj)
        {
            Debug.Log("Collision stay");
        }
    }
}

