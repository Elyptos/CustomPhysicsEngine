using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Phys
{
    public class PhysicsEngine : MonoSingleton<PhysicsEngine>
    {
        private static readonly uint ID_ALLOC_AMOUNT = 1000;
        private static readonly uint LAST_ID = 65535;

        static PhysicsEngine() { }
        private PhysicsEngine() { }

        private static Stack<uint> idPool = new Stack<uint>();
        private static uint highestId = 0;

        private static Dictionary<PhysComponent, uint> idLookUp = new Dictionary<PhysComponent, uint>();
        private static Dictionary<PhysCompoundCollider, LinkedListNode<PhysCompoundCollider>> compoundColliderReg = new Dictionary<PhysCompoundCollider, LinkedListNode<PhysCompoundCollider>>();
        private static Dictionary<PhysRigidbody, LinkedListNode<PhysRigidbody>> rigidbodyReg = new Dictionary<PhysRigidbody, LinkedListNode<PhysRigidbody>>();
        private static LinkedList<PhysCompoundCollider> colliderList = new LinkedList<PhysCompoundCollider>();
        private static LinkedList<PhysRigidbody> rigidbodiesList = new LinkedList<PhysRigidbody>();

        private ConcurrentDictionary<uint, byte> CollisionPairs = new ConcurrentDictionary<uint, byte>();

        public Vector2 Gravity = new Vector2(0.0f, -9.81f);
        public bool ParallelOperation = true;

        private Dictionary<int, bool> collisionMatrix = new Dictionary<int, bool>();

        private static bool _isRunning;

        public static PhysicsEventManager EventManager { get; set; } = new PhysicsEventManager();

        public static bool IsRunning
        {
            get { return _instance != null && _isRunning; }
        }

        public static void ApplyExplosion(Vector2 origin, float intensity, float radius, float falloffRadius)
        {
            float radSqr = intensity * intensity;
            float fradSqr = Mathf.Max(falloffRadius * falloffRadius, 0.001f);

            Parallel.ForEach(rigidbodiesList, rigid =>
            {
                float lenSqr = (origin - rigid.MassPoint).sqrMagnitude;
                float forceInt = (1.0f - Mathf.Clamp((lenSqr - radSqr) / fradSqr, 0f, 1f)) * intensity;

                rigid.AddForce((rigid.MassPoint - origin).normalized * forceInt, rigid.MassPoint);
            });
        }

        public static bool IsLayerCollisionAllowed(int layer1, int layer2)
        {
            int index = (layer1 << 6) | layer2;

            return Instance.collisionMatrix[index];
        }

        public static void RegisterCollider(PhysCompoundCollider coll)
        {
            if (compoundColliderReg.ContainsKey(coll))
                return;

            if(idPool.Count == 0)
            {
                FillIDPool();

                if (idPool.Count == 0)
                    return;
            }

            uint id = idPool.Pop();

            compoundColliderReg.Add(coll, colliderList.AddLast(coll));
            idLookUp.Add(coll, id);
        }

        public static void UnregisterCollider(PhysCompoundCollider coll)
        {
            if (!idLookUp.ContainsKey(coll))
                return;

            uint id = idLookUp[coll];

            idPool.Push(id);
            idLookUp.Remove(coll);

            colliderList.Remove(compoundColliderReg[coll]);
            compoundColliderReg.Remove(coll);
        }

        public static void RegisterRigidbody(PhysRigidbody rigid)
        {
            if (rigidbodyReg.ContainsKey(rigid))
                return;

            if (idPool.Count == 0)
            {
                FillIDPool();

                if (idPool.Count == 0)
                    return;
            }

            uint id = idPool.Pop();

            rigidbodyReg.Add(rigid, rigidbodiesList.AddLast(rigid));
            idLookUp.Add(rigid, id);
        }

        public static void UnregisterRigidbody(PhysRigidbody rigid)
        {
            if (!idLookUp.ContainsKey(rigid))
                return;

            uint id = idLookUp[rigid];

            idPool.Push(id);
            idLookUp.Remove(rigid);

            rigidbodiesList.Remove(rigidbodyReg[rigid]);
            rigidbodyReg.Remove(rigid);
        }

        private void OnEnable()
        {
            StartEngine();
        }

        private void OnDisable()
        {
            StopEngine();
        }

        private static void FillIDPool()
        {
            for(uint i = 0; i < ID_ALLOC_AMOUNT; i++)
            {
                uint id = i + highestId;

                if (id > LAST_ID)
                {
                    highestId = id;
                    return;
                }

                idPool.Push(id);
            }

            highestId += ID_ALLOC_AMOUNT;
        }

        public void StartEngine()
        {
            if (IsRunning)
                StopEngine();

            CopyUnityCollisionMatrix();

            StartCoroutine("PhysicsLoop");
        }

        public void StopEngine()
        {
            _isRunning = false;

            StopCoroutine("PhysicsLoop");
        }

        private void CopyUnityCollisionMatrix()
        {
            collisionMatrix.Clear();

            for(int i = 0; i < 32; i++)
            {
                for(int j = 0; j < 32; j++)
                {
                    int index = (i << 6) | j;

                    if(!collisionMatrix.ContainsKey(index))
                    {
                        collisionMatrix.Add(index, Physics.GetIgnoreLayerCollision(i, j));
                    }
                }
            }
        }

        private bool ShouldPerformCollisionDetection(uint a, uint b)
        {
            if (a == b)
                return false;

            uint collisionId = (a << 16) | b;

            if (CollisionPairs.ContainsKey(collisionId))
                return false;
            else
            {
                collisionId = (b << 16) | a;

                if (CollisionPairs.ContainsKey(collisionId))
                    return false;
                else
                {
                    CollisionPairs.TryAdd(collisionId, 0);
                    return true;
                }
            }
        }

        private IEnumerator PhysicsLoop()
        {
            _isRunning = true;

            while (_isRunning)
            {
                CollisionPairs.Clear();

                foreach(PhysRigidbody rigid in rigidbodiesList)
                {
                    rigid.UpdateAllCollider();
                    rigid.EvaluateBounds();
                    rigid.Warmup();
                }

                foreach(PhysCompoundCollider collider in colliderList)
                {
                    collider.UpdateAllCollider();
                    collider.EvaluateBounds();
                    collider.Warmup();
                }

                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = ParallelOperation ? -1 : 1;

                Parallel.ForEach(rigidbodiesList, options, rigid =>
                {
                    uint id1 = idLookUp[rigid];

                    foreach (PhysRigidbody rigid2 in rigidbodiesList)
                    {
                        if (IsLayerCollisionAllowed(rigid.CollisionLayer, rigid2.CollisionLayer))
                            continue;

                        if (ShouldPerformCollisionDetection(id1, idLookUp[rigid2]))
                        {
                            if(rigid.CachedBoundsWS.Intersects(rigid2.CachedBoundsWS))
                            {
                                rigid.GatherCollisionInformation(rigid2);
                            }
                        }
                    }

                    foreach(PhysCompoundCollider collider in colliderList)
                    {
                        if (IsLayerCollisionAllowed(rigid.CollisionLayer, collider.CollisionLayer))
                            continue;

                        if (ShouldPerformCollisionDetection(id1, idLookUp[collider]))
                        {
                            if (rigid.CachedBoundsWS.Intersects(collider.CachedBoundsWS))
                            {
                                rigid.GatherCollisionInformation(collider);
                            }
                        }
                    }
                });

                float deltaTime = Time.fixedDeltaTime;

                //Parallel.ForEach(rigidbodiesList, options, rigid =>
                //{
                //    rigid.PhysicsUpdate(deltaTime);
                //});

                //Iterate savely through list because destruction of entries can occur in this step.
                LinkedListNode<PhysRigidbody> e = rigidbodiesList.First;

                while(e != null)
                {
                    e.Value.PrePhysicsUpdate();

                    e = e.Next;
                }

                //foreach(PhysRigidbody rigid in rigidbodiesList)
                //{
                //    rigid.PrePhysicsUpdate();
                //}

                foreach(PhysRigidbody rigid in rigidbodiesList)
                {
                    rigid.PhysicsUpdate(deltaTime);
                }

                foreach(PhysRigidbody rigid in rigidbodiesList)
                {
                    rigid.PostPhysicsUpdate();
                }

                yield return new WaitForFixedUpdate();
            }
        }
    }

}
