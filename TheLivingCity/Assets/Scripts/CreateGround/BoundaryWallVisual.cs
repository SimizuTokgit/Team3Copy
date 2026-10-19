using UnityEngine;

//清水
//道の端の「ここから先は行けない」半透明の壁
//当たり判定は今までの見えない壁（LeftWall / RightWall）がやるので、これは見た目だけ
//地形の高さに沿って作り、プレイヤーが近づくほど濃くなる
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BoundaryWallVisual : MonoBehaviour
{
    private const float WallHeight = 6f;         //地面からの高さ
    private const float BaseAlpha = 0.18f;       //遠いときの濃さ
    private const float NearAlpha = 0.75f;       //すぐ横にいるときの濃さ
    private const float NearDistance = 10f;      //この距離から濃くなりはじめる
    private static readonly Color WallColor = new Color(1f, 0.45f, 0.1f);

    private static Material sharedMaterial;
    private static Transform player;

    private Mesh _mesh;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _block;

    /// <summary>
    /// 壁を作る（なければ作る）。edgeHeights は手前から奥への地面の高さ
    /// </summary>
    public static void Build(Transform chunk, string name, float localX, float[] edgeHeights, float step)
    {
        Transform t = chunk.Find(name);
        BoundaryWallVisual wall;
        if (t)
        {
            wall = t.GetComponent<BoundaryWallVisual>();
        }
        else
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(chunk, false);
            wall = go.AddComponent<BoundaryWallVisual>();
        }

        wall.transform.localPosition = new Vector3(localX, 0f, 0f);
        wall.BuildMesh(edgeHeights, step);
    }

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = GetMaterial();
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _block = new MaterialPropertyBlock();

        _mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    private void BuildMesh(float[] edgeHeights, float step)
    {
        int count = edgeHeights.Length;
        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uv = new Vector2[count * 2];
        int[] triangles = new int[(count - 1) * 6];

        for (int i = 0; i < count; i++)
        {
            float z = i * step;
            vertices[i * 2] = new Vector3(0f, edgeHeights[i] - 0.5f, z);
            vertices[i * 2 + 1] = new Vector3(0f, edgeHeights[i] + WallHeight, z);
            uv[i * 2] = new Vector2(z / 4f, 0f);
            uv[i * 2 + 1] = new Vector2(z / 4f, 1f);
        }

        for (int i = 0; i < count - 1; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
    }

    private void Update()
    {
        if (!player)
        {
            Player p = FindAnyObjectByType<Player>();
            if (!p) return;
            player = p.transform;
        }

        //近いほど濃く
        float distance = Mathf.Abs(player.position.x - transform.position.x);
        float near = 1f - Mathf.Clamp01(distance / NearDistance);
        Color c = WallColor;
        c.a = Mathf.Lerp(BaseAlpha, NearAlpha, near * near);

        _block.SetColor("_Color", c);
        _renderer.SetPropertyBlock(_block);
    }

    //下が濃くて上に行くほど消える、横じまの入った模様
    private static Material GetMaterial()
    {
        if (sharedMaterial) return sharedMaterial;

        const int w = 4;
        const int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < h; y++)
        {
            float fade = 1f - (float)y / (h - 1);
            float stripe = (y % 16) < 2 ? 1f : 0.45f;
            float edge = y < 3 ? 1f : 0f;
            float a = Mathf.Clamp01(fade * stripe + edge);
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        //Sprites/Default はビルドにも必ず入っていて、半透明・両面で描ける
        sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        sharedMaterial.mainTexture = tex;
        return sharedMaterial;
    }
}
