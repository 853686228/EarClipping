using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

#region Struct

public class EarClipping
{
    
    public List<Vector2[]> Trigulation(List<Vector2> points)
    {
        List<Vector2[]> res = new List<Vector2[]>();
        if (points.Count < 3)
            return res;
        
        List<Vector2> indics = new List<Vector2>(points);
        while (indics.Count > 3)
        {
            int count = indics.Count;
            int earIndex = GetAEarPointIndex(indics);
            Vector2[] tri = new Vector2[3]
                {  indics[(earIndex - 1 + count) % count],indics[earIndex], indics[(earIndex + 1) % count] }; 
            res.Add(tri);
            indics.RemoveAt(earIndex);
        }
        res.Add(new Vector2[3]{indics[0],indics[1],indics[2]});
        return res;
    }

    private int GetAEarPointIndex(List<Vector2> points)
    {
        int count = points.Count;
        for (int i = 0; i < count; ++i){
        
            if(IsEar(points[(i-1 +count )% count],points[i],points[(i+1)% count],points))
            {
                return i;
            }
        }

        return -1;
    }
    
    public bool IsConvexPoint(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        var crossRes = ab.x * ac.y - ab.y * ac.x;
        return crossRes < 0;
    }
    
    // ab x ap  bc x bp  ca x cp
    public bool IsInsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var c1 = (b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y);
        var c2 = (c.x - b.x) * (p.y - b.y) - (p.x - b.x) * (c.y - b.y);
        var c3 = (a.x - c.x) * (p.y - c.y) - (p.x - c.x) * (a.y - c.y);
        return (c1 <= 0 && c2 <= 0 && c3 <= 0) || (c1 >= 0 && c2 >= 0 && c3 >= 0);
    }
    
    //判断是否是一个耳朵
    public bool IsEar(Vector2 a, Vector2 b, Vector2 c, List<Vector2> points)
    {
        if (!IsConvexPoint(a, b, c))
            return false;
        for (int i = 0; i < points.Count; ++i)
        {
            if(points[i] == a || points[i] == b || points[i] == c)
                continue;
            if (IsInsideTriangle(a, b, c, points[i]))
                return false;
        }

        return true;
    }
    
}


#endregion
public class EarClippingTest : MonoBehaviour
{
    public List<Vector2> points;
    public bool drawOutLine = true;
    private EarClipping builder = new EarClipping();
    // Start is called before the first frame update
    void Start()
    {
        builder = new EarClipping();
    }

    private void OnDrawGizmos()
    {
        if(points.Count <=3)
            return;
        if(drawOutLine)
        {
            
            Gizmos.color = Color.green;
            for (int i = 0; i < points.Count; ++i)
            {
                if (i != points.Count-1)
                    Gizmos.DrawLine(points[i], points[i + 1]);
                else
                    Gizmos.DrawLine(points[i], points[0]);
            }
        }
        else
        {
            Gizmos.color = Color.red;
            var tris = builder.Trigulation(points);
            foreach (var tri in tris)
            {
                for (int i = 0; i < 3; ++i)
                    Gizmos.DrawLine(tri[i], tri[(i + 1) % 3]);
            }
        }
    }
}
