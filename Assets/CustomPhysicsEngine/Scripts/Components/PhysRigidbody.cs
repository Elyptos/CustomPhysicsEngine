using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phys
{
    public class PhysRigidbody : PhysCompoundCollider
    {
        public Vector2 Velocity;
        public Vector2 Acceleration;
        public Vector2 GravityMultiplier = Vector2.one;
        public float Mass = 1f;
        public float Bounciness = 0.9f;
        public float VolumeDensity = 1.2f;
        public float DragCoefficient = 0.45f;

        private static readonly float POSITION_CORRECTION_MOD = 0.2f;
        private static readonly float SLOP = 0.01f;
        private static readonly float VELOCITY_CLAMP_SQRT = 0.01f;

        private List<Vector2> positionCorrection = new List<Vector2>();
        private List<Vector2> velocityToAdd = new List<Vector2>();
        //private Vector2 gravity;
        //private Vector2 gravityToAdd;

        //Verlet integration
        private Vector2 force;
        private Vector2 fWeight;
        private Vector2 fVolumeRes;
        private float ObjectArea;
        private bool impulseApplied;

        private float deltaTime;

        private struct FRawCollision
        { 
            public CollisionContact CollisionContact;
            public bool IsBodyA;
        }

        private struct FCollisionBodyExtractor
        {
            public float Mass;
            public float Bounciness;
            public Vector2 Velocity;
            public PhysCompoundCollider Body;
            public bool IsRigidbody;

            public FCollisionBodyExtractor(PhysCompoundCollider compoundCollider)
            {
                if(compoundCollider is PhysRigidbody)
                {
                    PhysRigidbody rigid = compoundCollider as PhysRigidbody;

                    Mass = rigid.Mass;
                    Bounciness = rigid.Bounciness;
                    Velocity = rigid.Velocity;
                    IsRigidbody = true;
                }
                else
                {
                    Mass = 0f;
                    Bounciness = float.MaxValue;
                    Velocity = Vector2.zero;
                    IsRigidbody = false;
                }

                Body = compoundCollider;
            }
        }

        private ConcurrentStack<FRawCollision> collisions = new ConcurrentStack<FRawCollision>();

        protected override void OnRegister()
        {
            PhysicsEngine.RegisterRigidbody(this);
        }

        protected override void OnUnregister()
        {
            PhysicsEngine.UnregisterRigidbody(this);
        }

        public void RegisterCollision(CollisionContact collision, bool isBodyA)
        {
            collisions.Push(new FRawCollision() { CollisionContact = collision, IsBodyA = isBodyA });
        }

        public void GatherCollisionInformation(PhysCompoundCollider other)
        {
            if(other is PhysRigidbody)
            {
                PhysRigidbody rigid = other as PhysRigidbody;

                for (int c1 = 0; c1 < Collider.Length; c1++)
                {
                    FAABB2D bounds = other.Collider[c1].CachedBounds;

                    for (int c2 = 0; c2 < other.Collider.Length; c2++)
                    {
                        if (bounds.Intersects(other.Collider[c2].CachedBounds))
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
                    FAABB2D bounds = other.Collider[c1].CachedBounds;

                    for (int c2 = 0; c2 < other.Collider.Length; c2++)
                    {
                        if (bounds.Intersects(other.Collider[c2].CachedBounds))
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

        private void FixedUpdate()
        {
            
        }

        public override void Warmup()
        {
            base.Warmup();

            impulseApplied = false;
        }

        public override void PhysicsUpdate(float deltaTime)
        {
            base.PhysicsUpdate(deltaTime);

            positionCorrection.Clear();
            velocityToAdd.Clear();

            this.deltaTime = deltaTime;

            fWeight = Mass * PhysicsEngine.Instance.Gravity.Multiply(GravityMultiplier);
            ObjectArea = CachedBounds.Area;

            force += fWeight;

            //positionCorrection.Clear();
            //gravityToAdd = (PhysicsEngine.Instance.Gravity.Multiply(GravityMultiplier) * deltaTime);
            //gravity = gravityToAdd;

            ResolveCollision();

            if (Velocity.sqrMagnitude <= VELOCITY_CLAMP_SQRT)
                Velocity = Vector2.zero;

            fVolumeRes = (VolumeDensity * DragCoefficient * Velocity * Velocity) * 0.5f;
            fVolumeRes = -Velocity.normalized * fVolumeRes.magnitude;
            force += fVolumeRes;

            //Velocity += gravityToAdd;
        }

        public override void PostPhysicsUpdate()
        {
            Acceleration = force / Mass;

            Velocity += Acceleration * Time.fixedDeltaTime;

            if(velocityToAdd.Count > 0)
            {
                Vector2 impulse = Vector2.zero;

                foreach (var elem in velocityToAdd)
                {
                    impulse += elem;
                }

                impulse /= velocityToAdd.Count;

                Velocity += impulse;
            }

            transform.position += (Vector3)(Velocity * Time.fixedDeltaTime);

            force = Vector2.zero;

            if (Velocity.sqrMagnitude <= VELOCITY_CLAMP_SQRT)
                Velocity = Vector2.zero;

            //transform.position = transform.position + (Vector3)Velocity * Time.fixedDeltaTime;


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
                Vector2 edgeNormalColl = coll.IsBodyA ? coll.CollisionContact.EdgeNormalB : coll.CollisionContact.EdgeNormalA;
                Vector2 edgeNormal = coll.IsBodyA ? coll.CollisionContact.EdgeNormalA : coll.CollisionContact.EdgeNormalB;
                Vector2 collisionNormal = coll.CollisionContact.Normal;

                if(coll.IsBodyA && !coll.CollisionContact.BodyAInc || !coll.IsBodyA && coll.CollisionContact.BodyAInc)
                {
                    collisionNormal = -collisionNormal;
                }

                //if ((coll.IsBodyA && coll.CollisionContact.BodyAInc) || (!coll.IsBodyA && !coll.CollisionContact.BodyAInc))
                //{
                //    collisionNormal = -collisionNormal;
                //}

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
            float bounce = Mathf.Min(Bounciness, otherObject.Bounciness);

            Vector2 fN = edgeNormalColl * Vector2.Dot(force, edgeNormalColl);

            Vector2 impulsVel1 = Vector2.zero;
            Vector2 impulsVel2 = Vector2.zero;

            if (Vector2.Dot(Velocity - otherObject.Velocity, edgeNormalColl) < 0f)
            {
                Vector2 projVelocity = edgeNormalColl * Vector2.Dot(Velocity, edgeNormalColl);
                Vector2 projOtherVelocity = edgeNormalColl * Vector2.Dot(otherObject.Velocity, edgeNormalColl);

                impulsVel1 = CalculateImpulse(projVelocity, projOtherVelocity, Mass, otherObject.Mass, Bounciness, edgeNormalColl);

                if(otherObject.IsRigidbody)
                {
                    impulsVel2 = CalculateImpulse(projOtherVelocity, projVelocity, otherObject.Mass, Mass, otherObject.Bounciness, edgeNormal);

                    PhysRigidbody otherRigid = otherObject.Body as PhysRigidbody;

                    if (impulsVel2.sqrMagnitude != 0f)
                    {
                        if (Vector2.Dot(impulsVel2, edgeNormal) > 0)
                        {
                            otherRigid.ApplyImpulse(impulsVel2);
                        }
                        else
                        {
                            otherRigid.ApplyImpulse(-impulsVel2);
                        }
                    }
                    else
                    {
                        otherRigid.ApplyImpulse(-projOtherVelocity);
                    }
                }

                if (impulsVel1.sqrMagnitude != 0f)
                {
                    if (Vector2.Dot(impulsVel1, edgeNormalColl) > 0)
                    {
                        ApplyImpulse(impulsVel1);
                    }
                    else
                    {
                        ApplyImpulse(-impulsVel1);
                    }
                }
                else
                {
                    ApplyImpulse(-projVelocity);
                }
            }

            if(!impulseApplied)
            {
                float frictionCoef = Mathf.Min(Roughness * 0.1f, otherObject.Body.Roughness * 0.1f);
                Vector2 movementTangent = new Vector2(-edgeNormalColl.y, edgeNormalColl.x);
                Vector2 frictionVel;
                Vector2 friction;

                float tanDot = Vector2.Dot(movementTangent, Velocity);

                if (tanDot > 0)
                {
                    frictionVel = movementTangent * tanDot;
                    friction = movementTangent * frictionCoef;


                    if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
                    {
                        //velocityToAdd.Add(-friction);
                        ApplyImpulse(-friction);
                    }
                    else
                    {
                        //velocityToAdd.Add(-frictionVel);
                        ApplyImpulse(-frictionVel);
                    }
                }
                else if (tanDot < 0)
                {
                    frictionVel = -movementTangent * -tanDot;
                    friction = -movementTangent * frictionCoef;


                    if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
                    {
                        //velocityToAdd.Add(-friction);
                        ApplyImpulse(-friction);
                    }
                    else
                    {
                        //velocityToAdd.Add(-frictionVel);
                        ApplyImpulse(-frictionVel);
                    }
                }
            }

            if(Vector2.Dot(force, edgeNormalColl) < 0f)
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

        private Vector2 CalculateImpulse(Vector2 velA, Vector2 velB, float massA, float massB, float bounciness, Vector2 edgeNormal)
        {
            Vector2 impulse = Vector2.zero;

            if(massB == 0f)
            {
                impulse = velA;
            }
            else
            {
                impulse = (massA * velA + massB * (2 * -velB - velA)) / (massA + massB);
            }

            impulse = impulse * (1 + bounciness);

            return impulse;
        }

        protected void ApplyImpulse(Vector2 impulse)
        {
            Velocity += impulse;

            impulseApplied = true;
        }
    }
}

