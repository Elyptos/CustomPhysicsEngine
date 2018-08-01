using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Phys
{
    public struct FEdge
    {
        public Vector2 AV;
        public Vector2 BV;
        public int AI;
        public int BI;
        public Vector2 Normal;

        public FEdge(Vector2 av, Vector2 bv, int ai, int bi)
        {
            AV = av;
            BV = bv;
            AI = ai;
            BI = bi;
            Normal = Vector2.one;
        }
    }

    public static class MeshUtils
    {
        public static Vector2 CalcIntersection(Vector2 p1, Vector2 edgeDir, Vector2 rayPos, Vector2 rayNormal)
        {
            float lambda = -((Vector2.Dot(p1, rayNormal) - Vector2.Dot(rayPos, rayNormal)) / Vector2.Dot(rayNormal, edgeDir));

            return p1 + lambda * edgeDir;
        }

        public static Vector2 CalcNormalCC(Vector2 p1, Vector2 p2)
        {
            Vector2 dir = (p2 - p1).normalized;

            return new Vector2(dir.y, -dir.x);
        }

        public static Vector2 CalcNormal(Vector2 p1, Vector2 p2)
        {
            Vector2 dir = (p2 - p1).normalized;

            return new Vector2(-dir.y, dir.x);
        }
    }

    public static class CollMeshUtils
    {
        public static FEdge[] GetEdgeNeigbhours(FEdge edge, ref Vector2[] vertices)
        {
            int lIndex = LeftIndex(edge.AI, vertices.Length);
            int rIndex = RightIndex(edge.BI, vertices.Length);

            return new FEdge[2]
            {
                new FEdge(vertices[lIndex], edge.AV, lIndex, edge.AI),
                new FEdge(edge.BV, vertices[rIndex], edge.BI, rIndex)
            };
        }

        public static FEdge[] GetVertexEdges(int vi, ref Vector2[] vertices)
        {
            int lIndex = LeftIndex(vi, vertices.Length);
            int rIndex = RightIndex(vi, vertices.Length);

            return new FEdge[2]
            {
                new FEdge(vertices[lIndex], vertices[vi], lIndex, vi),
                new FEdge(vertices[vi], vertices[rIndex], vi, rIndex)
            };
        }

        //public static int GetIndexFromVertex(int v, ref int[] indices)
        //{
        //    for(int i = 0; i < indices.Length; i++)
        //    {
        //        if (indices[i] == v)
        //            return i;
        //    }

        //    return 0;
        //}

        private static int LeftIndex(int index, int indexBufferSize)
        {
            if (index == 0)
                return indexBufferSize - 1;
            else
                return index - 1;
        }

        private static int RightIndex(int index, int indexBufferSize)
        {
            if (index == indexBufferSize - 1)
                return 0;
            else
                return index + 1;
        }
    }
}

