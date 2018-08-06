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

        private static readonly float POSITION_CORRECTION_MOD = 0.9f;
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

        private float deltaTime;

        private struct FRawCollision
        {
            public Manifold Manifold;
            public bool IsReference;
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

        public void RegisterCollision(Manifold manifold, bool isReferenceRigidbody)
        {
            collisions.Push(new FRawCollision() { Manifold = manifold, IsReference = isReferenceRigidbody });
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
                            Manifold manifold = null;

                            if (Collider[c1].IsColliding(other.Collider[c2], out manifold))
                            {
                                manifold.A = this;
                                manifold.B = rigid;

                                RegisterCollision(manifold, true);
                                rigid.RegisterCollision(manifold, false);
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
                            Manifold manifold = null;

                            if (Collider[c1].IsColliding(other.Collider[c2], out manifold))
                            {
                                manifold.A = this;
                                manifold.B = null;
                                manifold.BComp = other;

                                RegisterCollision(manifold, true);
                            }
                        }
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            
        }

        public override void PhysicsUpdate(float deltaTime)
        {
            base.PhysicsUpdate(deltaTime);

            positionCorrection.Clear();
            velocityToAdd.Clear();

            this.deltaTime = deltaTime;

            fWeight = Mass * PhysicsEngine.Instance.Gravity.Multiply(GravityMultiplier);
            ObjectArea = CachedBounds.Area;

            fVolumeRes = (VolumeDensity * DragCoefficient * Velocity * Velocity) * 0.5f;
            fVolumeRes = -Velocity.normalized * fVolumeRes.magnitude;

            force += fWeight;
            force += fVolumeRes;

            //positionCorrection.Clear();
            //gravityToAdd = (PhysicsEngine.Instance.Gravity.Multiply(GravityMultiplier) * deltaTime);
            //gravity = gravityToAdd;

            ResolveCollision();

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
                for(int i = 0; i < coll.Manifold.Contacts.Length; i++)
                {
                    FContact contact = coll.Manifold.Contacts[i];
                    Vector2 edgeNormal = coll.IsReference ? coll.Manifold.EdgeNormalB : coll.Manifold.EdgeNormalA;

                    if ((coll.IsReference && coll.Manifold.BodyAInc) || (!coll.IsReference && !coll.Manifold.BodyAInc))
                    {
                        contact.Normal = -contact.Normal;
                    }

                    if(coll.Manifold.BComp != null)
                    {
                        ResolveContact(contact, 0f, float.MaxValue, edgeNormal, Vector2.zero, coll.Manifold.BComp);
                    }
                    else if(coll.IsReference)
                    {
                        ResolveContact(contact, coll.Manifold.B.Mass, coll.Manifold.B.Bounciness, edgeNormal, coll.Manifold.B.Velocity, coll.Manifold.B);
                    }
                    else
                    {
                        ResolveContact(contact, coll.Manifold.A.Mass, coll.Manifold.A.Bounciness, edgeNormal, coll.Manifold.A.Velocity, coll.Manifold.A);
                    }
                }
            }
        }

        private void ResolveContact(FContact contact, float otherMass, float otherBounciness, Vector2 edgeNormal, Vector2 otherVelocity, PhysCompoundCollider otherObject)
        {
            float bounce = Mathf.Min(Bounciness, otherBounciness);

            Vector2 fN = edgeNormal * Vector2.Dot(force, edgeNormal);

            Vector2 impulsVel = Vector2.zero;

            Vector2 projVelocity = edgeNormal * Vector2.Dot(Velocity, edgeNormal);
            Vector2 projOtherVelocity = edgeNormal * Vector2.Dot(otherVelocity, edgeNormal);

            if(otherMass == 0f)
            {
                impulsVel = projVelocity;
            }
            else
            {
                impulsVel = (Mass * projVelocity + otherMass * (2 * -projOtherVelocity - projVelocity)) / (Mass + otherMass);
            }

            impulsVel = impulsVel * (1 + Bounciness);

            if(impulsVel.sqrMagnitude != 0f)
            {
                if (Vector2.Dot(impulsVel, edgeNormal) > 0)
                {
                    //velocityToAdd.Add(impulsVel);
                    ApplyImpulse(impulsVel);
                }
                else
                {
                    //velocityToAdd.Add(-impulsVel);
                    ApplyImpulse(-impulsVel);
                }
            }
            else
            {
                velocityToAdd.Add(-projVelocity);

                float frictionCoef = Mathf.Min(Roughness * 0.1f, otherObject.Roughness * 0.1f);
                Vector2 movementTangent = new Vector2(-edgeNormal.y, edgeNormal.x);
                Vector2 frictionVel;
                Vector2 friction;

                float tanDot = Vector2.Dot(movementTangent, Velocity);

                if(tanDot > 0)
                {
                    frictionVel = movementTangent * tanDot;
                    friction = movementTangent * frictionCoef;
                }
                else if(tanDot < 0)
                {
                    frictionVel = -movementTangent * -tanDot;
                    friction = -movementTangent * frictionCoef;
                }
                else
                {
                    return;
                }

                if(frictionVel.sqrMagnitude > friction.sqrMagnitude)
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

            if(Vector2.Dot(force, edgeNormal) < 0f)
            {
                force -= fN;
            }

            positionCorrection.Add(contact.Normal * contact.Penetration * POSITION_CORRECTION_MOD);
        }

        private void ApplyImpulse(Vector2 impulse)
        {
            Velocity += impulse;
        }
    }
}

