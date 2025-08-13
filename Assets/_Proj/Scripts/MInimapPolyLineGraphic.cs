using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// UGUI 안에서 두께 있는 폴리라인을 그려주는 간단 컴포넌트
[RequireComponent(typeof(RectTransform))]
public class MinimapPolylineGraphic : MaskableGraphic
{
    public List<Vector2> points = new List<Vector2>(); // 로컬(UI) 좌표들(anchored 기준)
    public bool loop = true;
    [Range(1f, 20f)] public float thickness = 4f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Count < 2) return;

        int count = points.Count;
        for (int i = 0; i < count - 1 + (loop ? 1 : 0); i++)
        {
            Vector2 a = points[i % count];
            Vector2 b = points[(i + 1) % count];
            if ((b - a).sqrMagnitude < 1e-6f) continue;

            Vector2 dir = (b - a).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x); // 법선
            float w = thickness * 0.5f;

            Vector2 v0 = a + n * w;
            Vector2 v1 = a - n * w;
            Vector2 v2 = b - n * w;
            Vector2 v3 = b + n * w;

            int idx = vh.currentVertCount;
            UIVertex V(Vector2 p) { var v = UIVertex.simpleVert; v.position = p; v.color = color; return v; }
            vh.AddVert(V(v0)); vh.AddVert(V(v1)); vh.AddVert(V(v2)); vh.AddVert(V(v3));
            vh.AddTriangle(idx + 0, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx + 0);
        }
    }

    public void SetPoints(IList<Vector2> pts, bool isLoop, float thick = -1f)
    {
        points.Clear();
        if (pts != null) points.AddRange(pts);
        loop = isLoop;
        if (thick > 0f) thickness = thick;
        SetVerticesDirty();
    }
}