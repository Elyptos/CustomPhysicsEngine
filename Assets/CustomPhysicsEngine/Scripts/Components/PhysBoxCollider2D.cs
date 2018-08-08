using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Phys
{
    public class PhysBoxCollider2D : PhysCollider
    {
        [SerializeField]
        private Vector2 _size = Vector2.one;

        public Vector2 Size
        {
            get { return _size; }
            set
            {
                _size = value;
                IsDirty = true;
            }
        }


        //private Manifold manifold = null;

        public FCollPoly CollisionBodyWS
        {
            get;
            private set;
        }

        public FCollPoly CollisionBody
        {
            get;
            private set;
        }

        protected override Color ColliderColor
        {
            get { return Color.blue; }
        }

#if UNITY_EDITOR
        protected override void DrawCollider()
        {
            base.DrawCollider();

            Color fillColor = ColliderColor;

            fillColor.a = FILL_COLOR_ALPHA;

            UpdateCollisionBody_internal();
            CalculateWSCollisionBody();

            Handles.DrawSolidRectangleWithOutline(new Vector3[] {
                CollisionBodyWS.Vertices[0],
                CollisionBodyWS.Vertices[1],
                CollisionBodyWS.Vertices[2],
                CollisionBodyWS.Vertices[3]
            }, fillColor, ColliderColor);

            //if (manifold != null && manifold.Contacts != null)
            //{
            //    Handles.color = Color.red;

            //    foreach(var elem in manifold.Contacts)
            //    {
            //        Handles.DrawSolidDisc(elem.Position, Vector3.forward, 0.1f);
            //    }
            //}
        }
#endif

        protected override FAABB2D EvaluateBounds_internal()
        {
            if (!Application.isPlaying)
                UpdateCollisionBody_internal();

            Vector2 xAxis = Vector2.right;
            Vector2 yAxis = Vector2.up;

            Vector2 minMaxX = new Vector2();
            Vector2 minMaxY = new Vector2();

            minMaxX.x = Vector2.Dot(xAxis, CollisionBody.Vertices[0]);
            minMaxX.y = minMaxX.x;

            minMaxY.x = Vector2.Dot(yAxis, CollisionBody.Vertices[0]);
            minMaxY.y = minMaxY.x;

            for (int i = 0; i < CollisionBody.Vertices.Length; i++)
            {
                float d = Vector2.Dot(xAxis, CollisionBody.Vertices[i]);

                if (d < minMaxX.x)
                {
                    minMaxX.x = d;
                }
                else if (d > minMaxX.y)
                {
                    minMaxX.y = d;
                }

                d = Vector2.Dot(yAxis, CollisionBody.Vertices[i]);

                if (d < minMaxY.x)
                {
                    minMaxY.x = d;
                }
                else if (d > minMaxY.y)
                {
                    minMaxY.y = d;
                }
            }

            needsBoundsUpdate = false;

            return new FAABB2D()
            {
                Min = new Vector2(minMaxX.x, minMaxY.x),
                Max = new Vector2(minMaxX.y, minMaxY.y)
            };
        }

        public override void CalculateWSCollisionBody()
        {
            CollisionBodyWS = new FCollPoly()
            {
                Vertices = new Vector2[CollisionBody.Vertices.Length]
            };

            for(int i = 0; i < CollisionBody.Vertices.Length; i++)
            {
                CollisionBodyWS.Vertices[i] = CollisionBody.Vertices[i] + (Vector2)transform.position;
            }
        }

        protected override void UpdateCollisionBody_internal()
        {
            FCollRect rect = new FCollRect() { Offset = Offset, Size = Size };

            rect.TRS = FMatrix3x3.CreateTRS(Vector2.zero, transform.rotation.eulerAngles.z, transform.localScale);

            CollisionBody = rect.ToPoly();
        }

        protected override void UpdateBodyInformation_internal(float density)
        {
            Inertia = 0f;
            Mass = 0f;
            Area = 0f;

            for(int i = 0; i < CollisionBody.Vertices.Length; i++)
            {
                Vector2 a = CollisionBody.Vertices[i];
                Vector2 b = CollisionBody.Vertices[CollMeshUtils.RightIndex(i, CollisionBody.Vertices.Length)];

                float areaTri = Mathf.Abs(a.Cross(b)) * 0.5f;
                float massTri = density * areaTri;

                Area += areaTri;
                Mass += massTri;
                Inertia += massTri * (a.sqrMagnitude + b.sqrMagnitude + Vector2.Dot(a, b)) / 6f;
            }
        }

        public override bool IsColliding(PhysCollider collider, out CollisionContact manifold, out bool isBodyA)
        {
            if(collider is PhysBoxCollider2D)
            {
                PhysBoxCollider2D other = collider as PhysBoxCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingRect(CollisionBodyWS, other.CollisionBodyWS, out manifold);
            }
            else if (collider is PhysSphereCollider2D)
            {
                PhysSphereCollider2D other = collider as PhysSphereCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingRect(CollisionBodyWS, other.CollisionBodyWS, out manifold);
            }
            else if (collider is PhysPolyCollider2D)
            {
                PhysPolyCollider2D other = collider as PhysPolyCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingPoly(CollisionBodyWS, other.CollisionBodyWS, out manifold);
            }

            manifold = null;
            isBodyA = true;
            return false;
        }
    }
}

