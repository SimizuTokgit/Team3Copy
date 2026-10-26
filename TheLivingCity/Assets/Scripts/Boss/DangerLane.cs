using System.Collections.Generic;
using UnityEngine;

//清水
//電車が通る「攻撃範囲」を見せる帯
//地面の凸凹に沿った赤い帯（進む向きに流れる矢印つき）と、両はしの半透明の赤い壁で、幅と向きがひと目で分かるようにする
//電車が通りすぎたところは消していく（SetStartPoint）
public class DangerLane : MonoBehaviour
{
    private const float RowStep = 1.5f;      //地面の高さを測る間隔（m）
    private const float Lift = 0.35f;        //地面から少し浮かせる（凸凹に埋まらないように）
    private const float ScrollSpeed = 1.6f;  //矢印が流れる速さ

    private static Material fillMaterial;
    private static Material wallMaterial;

    private readonly List<Vector3> _left = new List<Vector3>();
    private readonly List<Vector3> _right = new List<Vector3>();
    private readonly List<float> _dist = new List<float>();

    private Vector3 _origin;
    private Vector3 _dir;
    private float _width;
    private float _wallHeight;
    private float _start;
    private bool _alert;
    private Color _color;

    private Mesh _fillMesh;
    private Mesh _wallMesh;
    private Renderer _fillRenderer;
    private Renderer _wallRenderer;
    private MaterialPropertyBlock _block;

    /// <summary>
    /// from から to まで、幅 width の帯を作る（ワールド座標。高さは地面に合わせる）
    /// </summary>
    public static DangerLane Create(Vector3 from, Vector3 to, float width, Color color, float wallHeight = 3.5f)
    {
        GameObject obj = new GameObject("DangerLane");
        DangerLane lane = obj.AddComponent<DangerLane>();
        lane.Setup(from, to, width, color, wallHeight);
        return lane;
    }

    private void Setup(Vector3 from, Vector3 to, float width, Color color, float wallHeight)
    {
        _color = color;
        _width = width;
        _wallHeight = wallHeight;
        _origin = new Vector3(from.x, 0f, from.z);
        Vector3 flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
        float length = flat.magnitude;
        _dir = length > 0.01f ? flat / length : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, _dir) * (width * 0.5f);

        //地面の高さを測って、帯の左右のふちを並べておく
        int rows = Mathf.Max(2, Mathf.CeilToInt(length / RowStep) + 1);
        float lastLeft = from.y, lastRight = from.y;
        for (int i = 0; i < rows; i++)
        {
            float d = Mathf.Min(length, i * RowStep);
            Vector3 c = _origin + _dir * d;
            lastLeft = GroundHeight(c - side, lastLeft);
            lastRight = GroundHeight(c + side, lastRight);
            //帯の真ん中が盛り上がっていても埋まらないように、高いほうに合わせる
            float mid = GroundHeight(c, Mathf.Max(lastLeft, lastRight));
            float y = Mathf.Max(mid, Mathf.Max(lastLeft, lastRight)) + Lift;
            _left.Add(new Vector3((c - side).x, y, (c - side).z));
            _right.Add(new Vector3((c + side).x, y, (c + side).z));
            _dist.Add(d);
        }

        _fillMesh = CreatePart("Fill", GetFillMaterial(), out _fillRenderer);
        _wallMesh = CreatePart("Wall", GetWallMaterial(), out _wallRenderer);
        _block = new MaterialPropertyBlock();
        Rebuild();
    }

    /// <summary>
    /// ここより手前（電車が通りすぎたところ）は消す
    /// </summary>
    public void SetStartPoint(Vector3 world)
    {
        _start = Mathf.Max(_start, Vector3.Dot(new Vector3(world.x, 0f, world.z) - _origin, _dir));
    }

    /// <summary>
    /// true で強く点滅（電車が走り出した）
    /// </summary>
    public void SetAlert(bool alert)
    {
        _alert = alert;
    }

    private Mesh CreatePart(string name, Material material, out Renderer renderer)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        Mesh mesh = new Mesh();
        mesh.MarkDynamic();
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = obj.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        renderer = r;
        return mesh;
    }

    private void LateUpdate()
    {
        Rebuild();

        //色と点滅
        float a = _alert ? Mathf.Lerp(0.65f, 1f, Mathf.PingPong(Time.time * 6f, 1f))
                         : Mathf.Lerp(0.3f, 0.5f, Mathf.PingPong(Time.time * 1.5f, 1f));
        Color c = _color;
        c.a = a;
        foreach (Renderer r in new[] { _fillRenderer, _wallRenderer })
        {
            r.GetPropertyBlock(_block);
            _block.SetColor("_Color", c);
            r.SetPropertyBlock(_block);
        }
    }

    //残っているところだけで帯と壁を作り直す
    private void Rebuild()
    {
        int first = 0;
        while (first < _dist.Count - 2 && _dist[first + 1] <= _start) first++;
        int count = _dist.Count - first;

        float scroll = Time.time * ScrollSpeed;
        var fv = new List<Vector3>(count * 2);
        var fu = new List<Vector2>(count * 2);
        var ft = new List<int>(count * 6);
        var wv = new List<Vector3>(count * 4);
        var wu = new List<Vector2>(count * 4);
        var wt = new List<int>(count * 12);

        for (int k = 0; k < count; k++)
        {
            int i = first + k;
            Vector3 l = _left[i], r = _right[i];
            if (k == 0 && _start > _dist[i])
            {
                //最初の列は、電車の先頭の位置までずらす
                float t = Mathf.InverseLerp(_dist[i], _dist[i + 1], _start);
                l = Vector3.Lerp(_left[i], _left[i + 1], t);
                r = Vector3.Lerp(_right[i], _right[i + 1], t);
            }
            float v = (Vector3.Dot(new Vector3(l.x, 0f, l.z) - _origin, _dir) + 0f) / Mathf.Max(0.5f, _width) - scroll;
            fv.Add(l); fu.Add(new Vector2(0f, v));
            fv.Add(r); fu.Add(new Vector2(1f, v));

            float wd = _dist[i] / 4f;
            wv.Add(l); wu.Add(new Vector2(wd, 0f));
            wv.Add(l + Vector3.up * _wallHeight); wu.Add(new Vector2(wd, 1f));
            wv.Add(r); wu.Add(new Vector2(wd, 0f));
            wv.Add(r + Vector3.up * _wallHeight); wu.Add(new Vector2(wd, 1f));

            if (k > 0)
            {
                int a = (k - 1) * 2, b = k * 2;
                ft.Add(a); ft.Add(b); ft.Add(a + 1);
                ft.Add(a + 1); ft.Add(b); ft.Add(b + 1);

                int p = (k - 1) * 4, q = k * 4;
                //左の壁
                wt.Add(p); wt.Add(p + 1); wt.Add(q);
                wt.Add(q); wt.Add(p + 1); wt.Add(q + 1);
                //右の壁
                wt.Add(p + 2); wt.Add(q + 2); wt.Add(p + 3);
                wt.Add(p + 3); wt.Add(q + 2); wt.Add(q + 3);
            }
        }

        _fillMesh.Clear();
        _fillMesh.SetVertices(fv);
        _fillMesh.SetUVs(0, fu);
        _fillMesh.SetTriangles(ft, 0);
        _fillMesh.RecalculateBounds();

        _wallMesh.Clear();
        _wallMesh.SetVertices(wv);
        _wallMesh.SetUVs(0, wu);
        _wallMesh.SetTriangles(wt, 0);
        _wallMesh.RecalculateBounds();
    }

    //いちばん低い地面（高架やジャンプ台の上ではなく道路の面）
    private static float GroundHeight(Vector3 p, float fallback)
    {
        int mask = LayerMask.GetMask("Ground");
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(p.x, fallback + 300f, p.z), Vector3.down, 700f, mask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return fallback;
        float y = float.MaxValue;
        foreach (RaycastHit h in hits) y = Mathf.Min(y, h.point.y);
        return y;
    }

    //帯の模様：うすい赤の地に、両はしの線と、進む向きの「＞」（白い画像を _Color で赤くする）
    private static Material GetFillMaterial()
    {
        if (fillMaterial) return fillMaterial;
        const int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                float a = 0.32f;
                if (u < 0.07f || u > 0.93f) a = 1f; //ふち
                float chevron = Mathf.Repeat(v - Mathf.Abs(u - 0.5f) * 0.9f, 1f);
                if (u > 0.15f && u < 0.85f && chevron < 0.22f) a = 0.85f; //矢印
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();
        fillMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
        return fillMaterial;
    }

    //壁の模様：下が濃く、上にいくほど消える
    private static Material GetWallMaterial()
    {
        if (wallMaterial) return wallMaterial;
        const int h = 64;
        Texture2D tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float k = 1f - (float)y / (h - 1);
            float a = y < 3 ? 1f : 0.6f * k * k;
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        wallMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
        return wallMaterial;
    }
}
