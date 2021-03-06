﻿using System.Collections.Generic;

using UnityEngine;

using MysteryCommand.Procedural.Mesh;

public class MeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;
    public MeshFilter walls;
    public MeshFilter cave;

    List<Vector3> vertices;
    List<int> triangles;

    Dictionary<int,List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(Texture2D map, float squareSize)
    {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); ++x)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); ++y)
            {
                TriangulateSquare(squareGrid.squares[x,y]);
            }
        }

        Mesh mesh = new Mesh();
        cave.mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i =0; i < vertices.Count; ++i)
        {
            float percentX = Mathf.InverseLerp(
                -map.width / 2 * squareSize,
                map.width / 2 * squareSize,
                vertices[i].x
            ) * tileAmount;

            float percentY = Mathf.InverseLerp(
                -map.width / 2 * squareSize,
                map.width / 2 * squareSize,
                vertices[i].z
            ) * tileAmount;

            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;

        Generate2DColliders();
    }

    void Generate2DColliders()
    {
        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; ++i)
        {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutlines();

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i =0; i < outline.Count; ++i)
            {
                edgePoints[i] = new Vector2(
                    vertices[outline[i]].x,
                    vertices[outline[i]].z
                );
            }
            edgeCollider.points = edgePoints;
        }
    }

    void TriangulateSquare(Square square)
    {
        switch (square.config)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.ml, square.bc, square.bl);
                break;
            case 2:
                MeshFromPoints(square.br, square.bc, square.mr);
                break;
            case 4:
                MeshFromPoints(square.tr, square.mr, square.tc);
                break;
            case 8:
                MeshFromPoints(square.tl, square.tc, square.ml);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.mr, square.br, square.bl, square.ml);
                break;
            case 6:
                MeshFromPoints(square.tc, square.tr, square.br, square.bc);
                break;
            case 9:
                MeshFromPoints(square.tl, square.tc, square.bc, square.bl);
                break;
            case 12:
                MeshFromPoints(square.tl, square.tr, square.mr, square.ml);
                break;
            case 5:
                MeshFromPoints(square.tc, square.tr, square.mr, square.bc, square.bl, square.ml);
                break;
            case 10:
                MeshFromPoints(square.tl, square.tc, square.mr, square.br, square.bc, square.ml);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.tc, square.tr, square.br, square.bl, square.ml);
                break;
            case 11:
                MeshFromPoints(square.tl, square.tc, square.mr, square.br, square.bl);
                break;
            case 13:
                MeshFromPoints(square.tl, square.tr, square.mr, square.bc, square.bl);
                break;
            case 14:
                MeshFromPoints(square.tl, square.tr, square.br, square.bc, square.ml);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.tl, square.tr, square.br, square.bl);
                checkedVertices.Add(square.tl.vertexIndex);
                checkedVertices.Add(square.tr.vertexIndex);
                checkedVertices.Add(square.br.vertexIndex);
                checkedVertices.Add(square.bl.vertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3) CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4) CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5) CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6) CreateTriangle(points[0], points[4], points[5]);
    }

    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; ++i)
        {
            if (points[i].vertexIndex == -1) {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    void CalculateMeshOutlines()
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);

                    FollowOutline(newOutlineVertex, outlines.Count-1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);

        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);
        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; ++i)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; ++j)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; ++i)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount ++;
                if (sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }

        return sharedTriangleCount == 1;
    }
}