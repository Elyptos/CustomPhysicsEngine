﻿using System.Collections;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Phys
{
    public class PhysPolyCollider2D : PhysCollider
    {
        public List<Vector2> Vertices = new List<Vector2>();

        //private Manifold manifold = null;

        public FCollPoly CollisionBody
        {
            get;
            private set;
        }

        protected override Color ColliderColor
        {
            get { return Color.yellow; }
        }

#if UNITY_EDITOR
        protected override void DrawCollider()
        {
            base.DrawCollider();

            List<Vector3> verts = CollisionBody.Vertices.ToList().ConvertAll(x => (Vector3)x);

            if (verts.Count == 0)
                return;

            verts.Add(verts.First());

            Color fillColor = ColliderColor;

            fillColor.a = FILL_COLOR_ALPHA;

            UpdateCollisionBody_internal();

            Handles.DrawPolyLine(verts.ToArray());

            Handles.color = fillColor;
            Handles.DrawAAConvexPolygon(verts.ToArray());
        }
#endif

        protected override FAABB2D EvaluateBounds_internal()
        {
            if(!Application.isPlaying)
            {
                FMatrix3x3 TRS = FMatrix3x3.CreateTRS(transform.position, transform.rotation.eulerAngles.z, transform.localScale);

                CollisionBody = new FCollPoly()
                {
                    Vertices = new Vector2[Vertices.Count]
                };

                for (int i = 0; i < Vertices.Count; i++)
                {
                    CollisionBody.Vertices[i] = TRS * Vertices[i];
                }
            }

            if (CollisionBody.Vertices.Length == 0)
                return new FAABB2D();

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

            return new FAABB2D()
            {
                Min = new Vector2(minMaxX.x, minMaxY.x),
                Max = new Vector2(minMaxX.y, minMaxY.y)
            };
        }

        protected override void UpdateCollisionBody_internal()
        {
            FMatrix3x3 TRS = FMatrix3x3.CreateTRS(transform.position, transform.rotation.eulerAngles.z, transform.localScale);

            CollisionBody = new FCollPoly()
            {
                Vertices = new Vector2[Vertices.Count]
            };

            for (int i = 0; i < Vertices.Count; i++)
            {
                CollisionBody.Vertices[i] = TRS * Vertices[i];
            }
        }

        public override bool IsColliding(PhysCollider collider, out CollisionContact manifold, out bool isBodyA)
        {
            if(collider is PhysBoxCollider2D)
            {
                PhysBoxCollider2D other = collider as PhysBoxCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingPoly(CollisionBody, other.CollisionBody, out manifold);
            }
            else if(collider is PhysPolyCollider2D)
            {
                PhysPolyCollider2D other = collider as PhysPolyCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingPoly(CollisionBody, other.CollisionBody, out manifold);
            }
            else if (collider is PhysSphereCollider2D)
            {
                PhysSphereCollider2D other = collider as PhysSphereCollider2D;

                isBodyA = true;
                return CollisionDetector.IsCollidingPoly(CollisionBody, other.CollisionBody, out manifold);
            }

            manifold = null;
            isBodyA = true;
            return false;
        }
    }
}
