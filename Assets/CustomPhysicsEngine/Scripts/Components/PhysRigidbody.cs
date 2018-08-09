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
        public float AngularVelocity;
        public Vector2 Acceleration;

        public Vector2 GravityMultiplier = Vector2.one;
        //public float Mass = 1f;
        public float Density = 1.175f;
        public float Bounciness = 0.9f;
        public float VolumeDensity = 1.2f;
        public float DragCoefficient = 0.45f;

        private static readonly float POSITION_CORRECTION_MOD = 0.2f;
        private static readonly float SLOP = 0.02f;
        private static readonly float VELOCITY_CLAMP_SQRT = 0.01f;

        private List<Vector2> positionCorrection = new List<Vector2>();
        private List<Vector2> velocityToAdd = new List<Vector2>();
        //private Vector2 gravity;
        //private Vector2 gravityToAdd;

        private float inertia;

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
            public float Bounciness;
            public Vector2 Velocity
            {
                get
                {
                    if(IsRigidbody)
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
                if(compoundCollider is PhysRigidbody)
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
            ObjectArea = CachedBoundsWS.Area;

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

            if (Velocity.sqrMagnitude <= VELOCITY_CLAMP_SQRT)
                Velocity = Vector2.zero;

            //if (Mathf.Abs(AngularVelocity) <= VELOCITY_CLAMP_SQRT)
            //    AngularVelocity = 0f;

            transform.position += (Vector3)(Velocity * Time.fixedDeltaTime);
            transform.rotation *= Quaternion.Euler(0f, 0f, AngularVelocity);

            force = Vector2.zero;

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

            //Vector2 impulsVel1 = Vector2.zero;
            //Vector2 impulsVel2 = Vector2.zero;

            //if (Vector2.Dot(Velocity - otherObject.Velocity, actualCollisionNormal) < 0f)
            //{
            //    Vector2 projVelocity = actualCollisionNormal * Vector2.Dot(Velocity, actualCollisionNormal);
            //    Vector2 projOtherVelocity = actualCollisionNormal * Vector2.Dot(otherObject.Velocity, actualCollisionNormal);

            //    impulsVel1 = CalculateImpulse(projVelocity, projOtherVelocity, Mass, otherObject.Mass, Bounciness);

            //    if(otherObject.IsRigidbody)
            //    {
            //        impulsVel2 = CalculateImpulse(projOtherVelocity, projVelocity, otherObject.Mass, Mass, otherObject.Bounciness);

            //        PhysRigidbody otherRigid = otherObject.Body as PhysRigidbody;

            //        if (impulsVel2.sqrMagnitude != 0f)
            //        {
            //            if (Vector2.Dot(impulsVel2, edgeNormal) > 0)
            //            {
            //                otherRigid.ApplyImpulse(impulsVel2);
            //            }
            //            else
            //            {
            //                otherRigid.ApplyImpulse(-impulsVel2);
            //            }
            //        }
            //        else
            //        {
            //            otherRigid.ApplyImpulse(-projOtherVelocity);
            //        }
            //    }

            //    if (impulsVel1.sqrMagnitude != 0f)
            //    {
            //        if (Vector2.Dot(impulsVel1, actualCollisionNormal) > 0)
            //        {
            //            ApplyImpulse(impulsVel1);
            //        }
            //        else
            //        {
            //            ApplyImpulse(-impulsVel1);
            //        }
            //    }
            //    else
            //    {
            //        ApplyImpulse(-projVelocity);
            //    }
            //}

            Vector2 startVel = Velocity;
            float startAngularVel = AngularVelocity;
            Vector2 startVelB = otherObject.Velocity;
            float startAngularVelB = otherObject.AngularVelocity;

            if (contact.ContactPoints == null)
                return;

            for(int i = 0; i < contact.ContactPoints.Length; i++)
            {
                Vector2 radA = contact.ContactPoints[i] - MassPoint;
                Vector2 radB = contact.ContactPoints[i] - otherObject.Body.MassPoint;

                Vector2 relVel = startVel + radA.Cross(startAngularVel) - startVelB - radB.Cross(startAngularVelB);

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

                    //relVel = velAImpulse + radA.Cross(anVelAImpulse) - velAImpulseB - radB.Cross(anVelAImpulseB);

                    //Vector2 movementTangent = relVel - (actualCollisionNormal * Vector2.Dot(relVel, actualCollisionNormal));
                    //movementTangent.Normalize();

                    //float tangentMag = -Vector2.Dot(relVel, movementTangent);
                    //tangentMag /= invMassSum;
                    //tangentMag /= contact.ContactPoints.Length;

                    //if (tangentMag.DeltaEquals(0f, 0.0001f))
                    //    return;

                    //Vector2 tangentImpulse;

                    //if (Mathf.Abs(tangentMag) < impulseScalar * frictionCoef)
                    //    tangentImpulse = movementTangent * tangentMag;
                    //else
                    //    tangentImpulse = movementTangent * -impulseScalar * frictionCoef;

                    //ApplyImpulse(tangentImpulse, radA);

                    //if (otherObject.IsRigidbody)
                    //{
                    //    ((PhysRigidbody)otherObject.Body).ApplyImpulse(-tangentImpulse, radB);
                    //}




                    //Vector2 movementTangent = new Vector2(-edgeNormalColl.y, edgeNormalColl.x);
                    //Vector2 frictionVel;
                    //Vector2 friction;

                    //float tanDot = Vector2.Dot(movementTangent, velAImpulse);

                    //if (tanDot > 0)
                    //{
                    //    frictionVel = (movementTangent * tanDot) / contact.ContactPoints.Length;
                    //    friction = (movementTangent * frictionCoef) / contact.ContactPoints.Length;


                    //    if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
                    //    {
                    //        //velocityToAdd.Add(-friction);
                    //        ApplyImpulse(-friction, radA);
                    //    }
                    //    else
                    //    {
                    //        //velocityToAdd.Add(-frictionVel);
                    //        ApplyImpulse(-frictionVel, radA);
                    //    }
                    //}
                    //else if (tanDot < 0)
                    //{
                    //    frictionVel = (-movementTangent * -tanDot) / contact.ContactPoints.Length;
                    //    friction = (-movementTangent * frictionCoef) / contact.ContactPoints.Length;


                    //    if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
                    //    {
                    //        //velocityToAdd.Add(-friction);
                    //        ApplyImpulse(-friction, radA);
                    //    }
                    //    else
                    //    {
                    //        //velocityToAdd.Add(-frictionVel);
                    //        ApplyImpulse(-frictionVel, radA);
                    //    }
                    //}
                }
            }

            //if (!impulseApplied)
            //{
            //    //float frictionCoef = Mathf.Min(Roughness * 0.1f, otherObject.Body.Roughness * 0.1f);
            //    Vector2 movementTangent = new Vector2(-edgeNormalColl.y, edgeNormalColl.x);
            //    Vector2 frictionVel;
            //    Vector2 friction;

            //    float tanDot = Vector2.Dot(movementTangent, Velocity);

            //    if (tanDot > 0)
            //    {
            //        frictionVel = movementTangent * tanDot;
            //        friction = movementTangent * frictionCoef;


            //        if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
            //        {
            //            //velocityToAdd.Add(-friction);
            //            ApplyImpulse(-friction);
            //        }
            //        else
            //        {
            //            //velocityToAdd.Add(-frictionVel);
            //            ApplyImpulse(-frictionVel);
            //        }
            //    }
            //    else if (tanDot < 0)
            //    {
            //        frictionVel = -movementTangent * -tanDot;
            //        friction = -movementTangent * frictionCoef;


            //        if (frictionVel.sqrMagnitude > friction.sqrMagnitude)
            //        {
            //            //velocityToAdd.Add(-friction);
            //            ApplyImpulse(-friction);
            //        }
            //        else
            //        {
            //            //velocityToAdd.Add(-frictionVel);
            //            ApplyImpulse(-frictionVel);
            //        }
            //    }
            //}

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

        private Vector2 CalculateImpulse(Vector2 velA, Vector2 velB, float massA, float massB, float bounciness)
        {
            Vector2 impulse = Vector2.zero;

            if(massB == 0f)
            {
                impulse = -velA;
            }
            else
            {
                impulse = (massA * velA + massB * (2 * -velB - velA)) / (massA + massB);
            }

            //impulse = impulse * (1 + bounciness);
            impulse = (velA - impulse) * bounciness;

            return impulse;
        }

        protected void ApplyImpulse(Vector2 impulse, Vector2 contact)
        {
            Velocity += InvMass * impulse;
            AngularVelocity += InvInertia * contact.Cross(impulse);

            impulseApplied = true;
        }
    }
}

