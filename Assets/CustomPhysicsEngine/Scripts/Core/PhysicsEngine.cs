using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public class PhysicsEngine : MonoSingleton<PhysicsEngine>
    {
        static PhysicsEngine() { }
        private PhysicsEngine() { }

        private static Dictionary<PhysCollider, LinkedListNode<PhysCollider>> colliderNodeReg = new Dictionary<PhysCollider, LinkedListNode<PhysCollider>>();
        private static Dictionary<PhysRigidbody, LinkedListNode<PhysRigidbody>> rigidbodyNodeReg = new Dictionary<PhysRigidbody, LinkedListNode<PhysRigidbody>>();
        private static LinkedList<PhysCollider> colliderReg = new LinkedList<PhysCollider>();
        private static LinkedList<PhysRigidbody> rigidbodiesReg = new LinkedList<PhysRigidbody>();

        private static bool _isRunning;

        public static bool IsRunning
        {
            get { return _instance != null && _isRunning; }
        }

        public static void RegisterCollider(PhysCollider coll)
        {
            if (colliderNodeReg.ContainsKey(coll))
                return;

            colliderNodeReg.Add(coll, colliderReg.AddLast(coll));
        }

        public static void UnregisterCollider(PhysCollider coll)
        {
            if (!colliderNodeReg.ContainsKey(coll))
                return;

            colliderReg.Remove(colliderNodeReg[coll]);
            colliderNodeReg.Remove(coll);
        }

        public static void RegisterRigidbody(PhysRigidbody rigid)
        {
            if (rigidbodyNodeReg.ContainsKey(rigid))
                return;

            rigidbodyNodeReg.Add(rigid, rigidbodiesReg.AddLast(rigid));
        }

        public static void UnregisterRigidbody(PhysRigidbody rigid)
        {
            if (!rigidbodyNodeReg.ContainsKey(rigid))
                return;

            rigidbodiesReg.Remove(rigidbodyNodeReg[rigid]);
            rigidbodyNodeReg.Remove(rigid);
        }

        private void OnEnable()
        {
            StartEngine();
        }

        private void OnDisable()
        {
            StopEngine();
        }

        public void StartEngine()
        {
            if (IsRunning)
                StopEngine();

            StartCoroutine("PhysicsLoop");
        }

        public void StopEngine()
        {
            _isRunning = false;

            StopCoroutine("PhysicsLoop");
        }

        private IEnumerator PhysicsLoop()
        {
            _isRunning = true;

            while (_isRunning)
            {
                foreach (PhysCollider collider1 in colliderReg)
                {
                    collider1.UpdateCollisionBody();

                    foreach (PhysCollider collider2 in colliderReg)
                    {
                        if (collider1 == collider2)
                            continue;

                        collider2.UpdateCollisionBody();

                        Debug.Log("Collision: " + collider1.IsColliding(collider2));
                    }

                    break;
                }

                yield return null;
            }
        }
    }

}
