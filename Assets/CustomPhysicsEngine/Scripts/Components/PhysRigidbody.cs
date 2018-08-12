using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Phys
{
    public class PhysRigidbody : PhysCompoundCollider
    {

        private struct FRawCollision
        {
            public CollisionContact CollisionContact;
            public bool IsBodyA;
        }

        private struct FCollisionBodyExtractor
        {
            public float Bounciness;
            public Vector2 Velocity
            {
                get
                {
                    if (IsRigidbody)
                    {
                        return ((PhysRigidbody)Body).Velocity;
                    }
                    else
                    {
                        return Vector2.zero;
                    }
                }
            }
            public float AngularVelocity
            {
                get
                {
                    if (IsRigidbody)
                    {
                        return ((PhysRigidbody)Body).AngularVelocity;
                    }
                    else
                    {
                        return 0f;
                    }
                }
            }
            public PhysCompoundCollider Body;
            public bool IsRigidbody;

            public FCollisionBodyExtractor(PhysCompoundCollider compoundCollider)
            {
                if (compoundCollider is PhysRigidbody)
                {
                    PhysRigidbody rigid = compoundCollider as PhysRigidbody;

                    Bounciness = rigid.Bounciness;
                    IsRigidbody = true;
                }
                else
                {
                    Bounciness = float.MaxValue;
                    IsRigidbody = false;
                }

                Body = compoundCollider;
            }
        }

        [System.Serializable]
        public struct FAxisLock
        {
            public bool LockX;
            public bool LockY;
            public bool LockRotation;
        }

        public Vector2 Velocity;
        public float AngularVelocity;
        public Vector2 Acceleration;
        public float AngularAcceleration;

        public Vector2 GravityMultiplier = Vector2.one;
        public float Density = 1.175f;
        public float Bounciness = 0.9f;
        public float VolumeDensity = 1.2f;
        public float DragCoefficient = 0.45f;

        public Vector2 ExternalForce { get; private set; }
        public float ExternalTorque { get; private set; }

        public FAxisLock LockAxis = new FAxisLock();

        private ConcurrentDictionary<PhysCompoundCollider, byte> componentsInCollision = new ConcurrentDictionary<PhysCompoundCollider, byte>();

        private static readonly float POSITION_CORRECTION_MOD = 0.2f;
        private static readonly float SLOP = 0.02f;
        private static readonly float VELOCITY_CLAMP_SQRT = 0.001f;
        private static readonly float ANGULAR_VELOCITY_CLAMP = 0.001f;

        private List<Vector2> positionCorrection = new List<Vector2>();

        private float inertia;

        private Vector2 force;
        private float torque;
        private Vector2 fWeight;
        private Vector2 fVolumeVelocityRes;
        private float ObjectArea;
        private bool impulseApplied;

        private float deltaTime;

        private ConcurrentStack<FRawCollision> collisions = new ConcurrentStack<FRawCollision>();

        public override void UpdateAllCollider()
        {
            Area = 0f;
            Mass = 0f;
            Inertia = 0f;

            for (int i = 0; i < Collider.Length; i++)
            {
                if(Collider[i].NeedsBodyUpdate())
                    Collider[i].UpdateCollisionBody(Density);

                Collider[i].CalculateWSCollisionBody();

                Area += Collider[i].Area;
                Mass += Collider[i].Mass;
                Inertia += Collider[i].Inertia;
            }

            InvMass = Mass == 0f ? 0f : 1f / Mass;
            InvInertia = Inertia == 0f ? 0f : 1f / Inertia;
        }

        protected override void OnRegister()
        {
            PhysicsEngine.RegisterRigidbody(this);

            IsValid = true;
        }

        protected override void OnUnregister()
        {
            IsValid = false;
            ExitFromAllCollisions();

            PhysicsEngine.UnregisterRigidbody(this);
        }

        public void AddForce(Vector2 force, Vector2 forceLocation)
        {
            ExternalForce += force;

            AddTorque(force, forceLocation);
        }

        public void AddTorque(Vector2 force, Vector2 forceLocation)
        {
            Vector2 rad = forceLocation - MassPoint;
            Vector2 normal = new Vector2(-force.y, force.x);

            if (!Vector2.Dot(normal, rad).DeltaEquals(0f, 0.01f))
            {
                ExternalTorque += rad.Cross(force);
            }
        }

        public void AddImpulse(Vector2 direction, float intensity, Vector2 impulseLocation)
        {
            ApplyImpulse(direction * intensity, impulseLocation - (Vector2)transform.position);
        }

        public void RegisterCollision(CollisionContact collision, bool isBodyA)
        {
            collisions.Push(new FRawCollision() { CollisionContact = collision, IsBodyA = isBodyA });

            if(!componentsInCollision.ContainsKey(isBodyA ? collision.B : collision.A))
            {
                componentsInCollision.TryAdd(isBodyA ? collision.B : collision.A, 0);
            }
        }

        public void GatherCollisionInformation(PhysCompoundCollider other)
        {
            if(other is PhysRigidbody)
            {
                PhysRigidbody rigid = other as PhysRigidbody;

                for (int c1 = 0; c1 < Collider.Length; c1++)
                {
                    FAABB2D bounds = other.Collider[c1].CachedBoundsWS;

                    for (int c2 = 0; c2 < other.Collider.Length; c2++)
                    {
                        if (bounds.Intersects(other.Collider[c2].CachedBoundsWS))
                        {
                            CollisionContact manifold = null;
                            bool isBodyA = false;

                            if (Collider[c1].IsColliding(other.Collider[c2], out manifold, out isBodyA))
                            {
                                if(isBodyA)
                                {
                                    manifold.A = this;
                                    manifold.B = rigid;
                                }
                                else
                                {
                                    manifold.A = rigid;
                                    manifold.B = this;
                                }

                                RegisterCollision(manifold, isBodyA);
                                rigid.RegisterCollision(manifold, !isBodyA);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int c1 = 0; c1 < Collider.Length; c1++)
                {
                    FAABB2D bounds = other.Collider[c1].CachedBoundsWS;

                    for (int c2 = 0; c2 < other.Collider.Length; c2++)
                    {
                        if (bounds.Intersects(other.Collider[c2].CachedBoundsWS))
                        {
                            CollisionContact manifold = null;
                            bool isBodyA = false;

                            if (Collider[c1].IsColliding(other.Collider[c2], out manifold, out isBodyA))
                            {
                                if(isBodyA)
                                {
                                    manifold.A = this;
                                    manifold.B = other;
                                }
                                else
                                {
                                    manifold.A = other;
                                    manifold.B = this;
                                }

                                RegisterCollision(manifold, isBodyA);
                            }
                        }
                    }
                }
            }
        }

        public override void Warmup()
        {
            base.Warmup();

            impulseApplied = false;
            componentsInCollision.Clear();
        }

        public override void PrePhysicsUpdate()
        {
            base.PrePhysicsUpdate();

            ConcurrentQueue<PhysCompoundCollider> colliderToTriggerExit = new ConcurrentQueue<PhysCompoundCollider>();
            ConcurrentQueue<PhysCompoundCollider> colliderToTriggerEnter = new ConcurrentQueue<PhysCompoundCollider>();

            Parallel.ForEach(collisionSet, elem =>
            {
                if (!componentsInCollision.ContainsKey(elem))
                {
                    colliderToTriggerExit.Enqueue(elem);
                }
            });

            Parallel.ForEach(componentsInCollision, elem =>
            {
                if (!collisionSet.Contains(elem.Key))
                {
                    colliderToTriggerEnter.Enqueue(elem.Key);
                }
            });

            PhysCompoundCollider coll;

            while (colliderToTriggerExit.TryDequeue(out coll))
            {
                TriggerCollisionExit(coll);
            }

            while(colliderToTriggerEnter.TryDequeue(out coll))
            {
                TriggerCollisionEnter(coll);
            }
        }

        public override void PhysicsUpdate(float deltaTime)
        {
            base.PhysicsUpdate(deltaTime);

            positionCorrection.Clear();

            this.deltaTime = deltaTime;

            fWeight = Mass * PhysicsEngine.Instance.Gravity.Multiply(GravityMultiplier);
            ObjectArea = CachedBoundsWS.Area;

            force += fWeight;
            force += ExternalForce;

            torque += ExternalTorque;

            ResolveCollision();

            if (Velocity.sqrMagnitude <= VELOCITY_CLAMP_SQRT)
                Velocity = Vector2.zero;

            fVolumeVelocityRes = (VolumeDensity * DragCoefficient * Velocity * Velocity) * 0.5f;
            fVolumeVelocityRes = -Velocity.normalized * fVolumeVelocityRes.magnitude;
            force += fVolumeVelocityRes;
        }

        public override void PostPhysicsUpdate()
        {
            Acceleration = force / Mass;
            AngularAcceleration = torque / Inertia;

            Velocity += Acceleration * Time.fixedDeltaTime;
            AngularVelocity += AngularAcceleration * Time.fixedDeltaTime;

            Velocity.x = LockAxis.LockX ? 0f : Velocity.x;
            Velocity.y = LockAxis.LockY ? 0f : Velocity.y;

            AngularVelocity = LockAxis.LockRotation ? 0f : AngularVelocity;

            if (Velocity.sqrMagnitude <= VELOCITY_CLAMP_SQRT)
                Velocity = Vector2.zero;

            //if (Mathf.Abs(AngularVelocity) <= ANGULAR_VELOCITY_CLAMP)
            //    AngularVelocity = 0f;

            transform.position += (Vector3)(Velocity * Time.fixedDeltaTime);
            transform.rotation *= Quaternion.Euler(0f, 0f, AngularVelocity);

            force = Vector2.zero;
            torque = 0f;
            ExternalForce = Vector2.zero;
            ExternalTorque = 0f;

            if (positionCorrection.Count > 0)
            {
                Vector2 posCorrection = Vector2.zero;

                foreach (var elem in positionCorrection)
                {
                    posCorrection += elem;
                }

                posCorrection /= positionCorrection.Count;

                transform.position += (Vector3)posCorrection;
            }
        }

        private void ResolveCollision()
        {
            FRawCollision coll;

            while (collisions.TryPop(out coll))
            {
                if (coll.CollisionContact.A.IsTrigger || coll.CollisionContact.B.IsTrigger)
                    continue;

                Vector2 edgeNormalColl = coll.IsBodyA ? coll.CollisionContact.EdgeNormalB : coll.CollisionContact.EdgeNormalA;
                Vector2 edgeNormal = coll.IsBodyA ? coll.CollisionContact.EdgeNormalA : coll.CollisionContact.EdgeNormalB;
                Vector2 collisionNormal = coll.CollisionContact.Normal;

                if(coll.IsBodyA && !coll.CollisionContact.BodyAInc || !coll.IsBodyA && coll.CollisionContact.BodyAInc)
                {
                    collisionNormal = -collisionNormal;
                }

                if (coll.IsBodyA)
                {
                    ResolveContact(coll.CollisionContact, collisionNormal, edgeNormalColl, edgeNormal, new FCollisionBodyExtractor(coll.CollisionContact.B));
                }
                else
                {
                    ResolveContact(coll.CollisionContact, collisionNormal, edgeNormalColl, edgeNormal, new FCollisionBodyExtractor(coll.CollisionContact.A));
                }
            }
        }

        private void ResolveContact(CollisionContact contact, Vector2 actualCollisionNormal, Vector2 edgeNormalColl, Vector2 edgeNormal, FCollisionBodyExtractor otherObject)
        {
            float bounce = (Bounciness + otherObject.Bounciness) * 0.5f;//Mathf.Min(Bounciness, otherObject.Bounciness);
            float frictionCoef = Mathf.Min(Roughness * 0.1f, otherObject.Body.Roughness * 0.1f);

            Vector2 fN = actualCollisionNormal * Vector2.Dot(force, actualCollisionNormal);

            Vector2 startVel = Velocity;
            float startAngularVel = AngularVelocity;
            Vector2 startVelB = otherObject.Velocity;
            float startAngularVelB = otherObject.AngularVelocity;

            if (contact.ContactPoints != null)
            {

                for (int i = 0; i < contact.ContactPoints.Length; i++)
                {
                    Vector2 radA = contact.ContactPoints[i] - MassPoint;
                    Vector2 radB = contact.ContactPoints[i] - otherObject.Body.MassPoint;

                    Vector2 relVel = startVel + radA.Cross(startAngularVel) - startVelB - radB.Cross(startAngularVelB);

                    //Vector2 relVel = startVel - startVelB;

                    float contactVel = Vector2.Dot(relVel, actualCollisionNormal);

                    if (contactVel < 0)
                    {
                        float invMassSum = InvMass + otherObject.Body.InvMass + Mathf.Pow(radA.Cross(actualCollisionNormal), 2) * InvInertia +
                            Mathf.Pow(radB.Cross(actualCollisionNormal), 2) * otherObject.Body.InvInertia;

                        float impulseScalar = -(1f + Bounciness) * contactVel;
                        impulseScalar /= invMassSum;
                        impulseScalar /= contact.ContactPoints.Length;

                        Vector2 impulse = actualCollisionNormal * impulseScalar;

                        ApplyImpulse(impulse, radA);

                        Vector2 velAImpulse = startVel + InvMass * impulse;
                        float anVelAImpulse = startAngularVel + InvInertia * radA.Cross(impulse);
                        Vector2 velAImpulseB = Vector2.zero;
                        float anVelAImpulseB = 0f;

                        if (otherObject.IsRigidbody)
                        {
                            ((PhysRigidbody)otherObject.Body).ApplyImpulse(-impulse, radB);

                            velAImpulseB = startVelB + otherObject.Body.InvMass * -impulse;
                            anVelAImpulseB = startAngularVelB + otherObject.Body.InvInertia * radB.Cross(-impulse);
                        }

                        Vector2 movementTangent = new Vector2(-edgeNormalColl.y, edgeNormalColl.x);
                        Vector2 frictionVel;
                        Vector2 friction;

                        float tanDot = Vector2.Dot(movementTangent, velAImpulse);

                        if (tanDot > 0)
                        {
                            frictionVel = (movementTangent * tanDot) / contact.ContactPoints.Length;
                            friction = (movementTangent * frictionCoef) / contact.ContactPoints.Length;


                            if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
                            {
                                ApplyImpulse(-friction, radA);
                            }
                            else
                            {
                                ApplyImpulse(-frictionVel, radA);
                            }
                        }
                        else if (tanDot < 0)
                        {
                            frictionVel = (-movementTangent * -tanDot) / contact.ContactPoints.Length;
                            friction = (-movementTangent * frictionCoef) / contact.ContactPoints.Length;


                            if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
                            {
                                ApplyImpulse(-friction, radA);
                            }
                            else
                            {
                                ApplyImpulse(-frictionVel, radA);
                            }
                        }
                    }
                }
            }

            if (Vector2.Dot(force, edgeNormalColl) < 0f)
            {
                force -= fN;
            }

            if(contact.Penetration > SLOP)
            {
                if (otherObject.IsRigidbody)
                    positionCorrection.Add(actualCollisionNormal * contact.Penetration * POSITION_CORRECTION_MOD * 0.5f);
                else
                    positionCorrection.Add(actualCollisionNormal * contact.Penetration * POSITION_CORRECTION_MOD);
            }
        }

        protected void ApplyImpulse(Vector2 impulse, Vector2 contact)
        {
            Velocity += InvMass * impulse;

            Vector2 normal = new Vector2(-impulse.y, impulse.x);

            //if(!Vector2.Dot(normal, contact).DeltaEquals(0f, 0.01f))
                AngularVelocity += InvInertia * contact.Cross(impulse);

            impulseApplied = true;
        }

        protected void ApplyTorque(Vector2 force, Vector2 rad)
        {
            Vector2 normal = new Vector2(-force.y, force.x);

            if (!Vector2.Dot(normal, rad).DeltaEquals(0f, 0.01f))
            {
                torque += rad.Cross(force);
            }
        }

        protected void TriggerCollisionEnter(PhysCompoundCollider other)
        {
            if(other.IsValid)
                collisionSet.Add(other);

            FCollision collision = new FCollision()
            {
                Other = other.gameObject,
                OtherCollider = other,
                OtherRigidbody = other as PhysRigidbody
            };

            if(other.IsTrigger || IsTrigger)
            {
                PhysicsEngine.EventManager.InvokeTriggerEnter(gameObject, collision);
            }
            else
            {
                PhysicsEngine.EventManager.InvokeCollisionEnter(gameObject, collision);
            }

            if(collision.OtherRigidbody == null)
            {
                other.RemoteRegisterCollision(this);

                collision = new FCollision()
                {
                    Other = this.gameObject,
                    OtherCollider = this,
                    OtherRigidbody = this
                };

                if (other.IsTrigger || IsTrigger)
                {
                    PhysicsEngine.EventManager.InvokeTriggerEnter(other.gameObject, collision);
                }
                else
                {
                    PhysicsEngine.EventManager.InvokeCollisionEnter(other.gameObject, collision);
                }
            }
        }

        protected void TriggerCollisionExit(PhysCompoundCollider other)
        {
            collisionSet.Remove(other);

            FCollision collision = new FCollision()
            {
                Other = other.gameObject,
                OtherCollider = other,
                OtherRigidbody = other as PhysRigidbody
            };

            if (other.IsTrigger || IsTrigger)
            {
                PhysicsEngine.EventManager.InvokeTriggerExit(gameObject, collision);
            }
            else
            {
                PhysicsEngine.EventManager.InvokeCollisionExit(gameObject, collision);
            }

            if (!(other is PhysRigidbody))
            {
                other.RemoteUnregisterCollision(this);

                collision = new FCollision()
                {
                    Other = this.gameObject,
                    OtherCollider = this,
                    OtherRigidbody = this
                };

                if (other.IsTrigger || IsTrigger)
                {
                    PhysicsEngine.EventManager.InvokeTriggerExit(other.gameObject, collision);
                }
                else
                {
                    PhysicsEngine.EventManager.InvokeCollisionExit(other.gameObject, collision);
                }
            }
        }
    }
}

