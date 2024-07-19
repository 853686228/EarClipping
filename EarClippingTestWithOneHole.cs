using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

#region Struct

public class EarClipping
{
    private bool IsConvexPoint(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        var cross = ab.x * ac.y - ab.y * ac.x;
        return cross < 0;
    }

    // ab x ap  bc x bp  ca x cp
    private bool IsInsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var c1 = (b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y);
        var c2 = (c.x - b.x) * (p.y - b.y) - (p.x - b.x) * (c.y - b.y);
        var c3 = (a.x - c.x) * (p.y - c.y) - (p.x - c.x) * (a.y - c.y);
        return (c1 <= 0 && c2 <= 0 && c3 <= 0) || (c1 >= 0 && c2 >= 0 && c3 >= 0);
    }

    public bool IsEar(Vector2 a, Vector2 b, Vector2 c, List<Vector2> points)
    {
        if (!IsConvexPoint(a, b, c))
            return false;
        for (int i = 0; i < points.Count; ++i)
        {
            if (points[i] == a || points[i] == b || points[i] == c)
                continue;
            if (IsInsideTriangle(a, b, c, points[i]))
                return false;
        }

        return true;
    }

    private int GetAEarPointIndex(List<Vector2> points)
    {
        int count = points.Count;
        for (int i = 0; i < count; ++i)
        {
            if (IsEar(points[(i - 1 + count) % count], points[i], points[(i + 1) % count], points))
            {
                return i;
            }
        }

        return -1;
    }

    public List<Vector2[]> Triangulation(List<Vector2> points)
    {
        List<Vector2[]> res = new List<Vector2[]>();
        if (points.Count < 3)
            return res;

        while (points.Count > 3)
        {
            int count = points.Count;
            int earIndex = GetAEarPointIndex(points);
            Vector2[] tri = new Vector2[3]
            {
                points[(earIndex - 1 + count) % count], points[earIndex], points[(earIndex + 1) % count]
            };
            res.Add(tri);
            points.RemoveAt(earIndex);
        }

        res.Add(new Vector2[3] { points[0], points[1], points[2] });
        return res;
    }

    public List<Vector2[]> Triangulation(List<List<Vector2>> points)
    {
        if (points.Count == 1)
            return Triangulation(new List<Vector2>(points[0]));
        
        List<Vector2> insideEdge = new List<Vector2>(points[1]);
        List<Vector2> outsideEdge = new List<Vector2>(points[0]);

        int outsideCount = outsideEdge.Count;
        int insideCount = insideEdge.Count;

        //构造桥连接内外多边形
        int rightmostIndex = GetMaxXPoint(insideEdge);
        int nearestOutsideIndex = GetNearestPointIndex(insideEdge[rightmostIndex], outsideEdge);
        List<Vector2> bridge = new List<Vector2>();
        for (int i = 0; i < insideEdge.Count; ++i)
        {
            bridge.Add(insideEdge[(rightmostIndex + i) % insideCount]);
        }

        bridge.Add(insideEdge[rightmostIndex]);
        bridge.Add(outsideEdge[nearestOutsideIndex]);

        outsideEdge.InsertRange(nearestOutsideIndex + 1, bridge);
        return Triangulation(outsideEdge);
    }

    private Mesh RebuildMesh(List<Vector2[]> tris)
    {
        Mesh mesh = new Mesh();

        // 将Vector2转换为Vector3，并处理顶点重复问题
        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesList = new List<int>();
        Dictionary<Vector2, int> vertexIndexMap = new Dictionary<Vector2, int>();

        int currentIndex = 0;
        foreach (var triangle in tris)
        {
            foreach (var vertex in triangle)
            {
                if (!vertexIndexMap.ContainsKey(vertex))
                {
                    vertexIndexMap[vertex] = currentIndex;
                    verticesList.Add(new Vector3(vertex.x, vertex.y, 0));
                    currentIndex++;
                }

                trianglesList.Add(vertexIndexMap[vertex]);
            }
        }

        // 设置顶点和三角形索引
        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
        // 计算法线
        mesh.RecalculateNormals();
        // 计算边界
        mesh.RecalculateBounds();
        return mesh;
    }

    private int GetMaxXPoint(List<Vector2> points)
    {
        int index = -1;
        float maxVal = float.MinValue;
        for (int i = 0; i < points.Count; ++i)
        {
            if (points[i].x > maxVal)
            {
                maxVal = points[i].x;
                index = i;
            }
        }

        return index;
    }

    private int GetNearestPointIndex(Vector2 point, List<Vector2> outsidePoints)
    {
        float distSquare = float.MaxValue;
        int index = -1;
        for (int i = 0; i < outsidePoints.Count; ++i)
        {
            float distVal = (outsidePoints[i].x - point.x) * (outsidePoints[i].x - point.x) +
                            (outsidePoints[i].y - point.y) * (outsidePoints[i].y - point.y);
            if (distVal < distSquare)
            {
                distSquare = distVal;
                index = i;
            }
        }

        return index;
    }
}

#endregion

public class EarClippingTest : MonoBehaviour
{
    [Header("外部点（顺时针）")] public List<Vector2> outsidePoints;
    [Header("内部点（逆时针）")] public List<Vector2> insidePoints;
    public bool drawOutLine = true;

    private EarClipping builder = new EarClipping();

    // Start is called before the first frame update
    void Start()
    {
        builder = new EarClipping();
    }

    private void OnDrawGizmos()
    {
        if (outsidePoints.Count <= 3)
            return;
        if (drawOutLine)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < outsidePoints.Count; ++i)
            {
                if (i != outsidePoints.Count - 1)
                    Gizmos.DrawLine(outsidePoints[i], outsidePoints[i + 1]);
                else
                    Gizmos.DrawLine(outsidePoints[i], outsidePoints[0]);
            }

            for (int i = 0; i < insidePoints.Count; ++i)
            {
                if (i != insidePoints.Count - 1)
                    Gizmos.DrawLine(insidePoints[i], insidePoints[i + 1]);
                else
                    Gizmos.DrawLine(insidePoints[i], insidePoints[0]);
            }
        }
        else
        {
            Gizmos.color = Color.red;
            List<Vector2[]> tris;
            if (insidePoints == null || insidePoints.Count < 3)
            {
                tris = builder.Triangulation(new List<Vector2>(outsidePoints));
            }
            else
            {
                tris = builder.Triangulation(new List<List<Vector2>>
                {
                    outsidePoints,
                    insidePoints
                });
            }

            foreach (var tri in tris)
            {
                for (int i = 0; i < 3; ++i)
                    Gizmos.DrawLine(tri[i], tri[(i + 1) % 3]);
            }
        }
    }
}