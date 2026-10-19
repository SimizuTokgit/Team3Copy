using UnityEngine;

//清水
//道の外側に崩れた街並みを並べる飾り
//今までは道の外が何も無く、細い地面が空に浮いているように見えていた
//当たり判定は消すので、ぶつかることはない（道の端には見えない壁がある）
public class MapDecoration : MonoBehaviour
{
    [SerializeField] private GameObject[] DecorPrefabs;    //建物・看板・木など
    [SerializeField] private float SideWidth = 90f;         //道の外にどれだけ広げるか
    [SerializeField] private float RoadMargin = 6f;         //道のすぐ横は少し空ける
    [SerializeField] private int DecorPerSide = 6;          //地面1枚・片側あたりの数
    [SerializeField] private Vector2 ScaleRange = new Vector2(1.2f, 2.8f);
    [SerializeField] private float MaxTilt = 18f;           //崩れて傾いている感じ
    [SerializeField] private float MaxSink = 3f;            //地面に沈めて崩れた感じ
    [SerializeField] private float SideGroundY = -0.3f;     //外側の地面の高さ

    //"Keep_" で始まる子は GroundAutomatically が作り直さずに使い回す
    private const string SideGroundLeft = "Keep_SideGroundL";
    private const string SideGroundRight = "Keep_SideGroundR";

    /// <summary>
    /// GroundAutomatically で作り直さない子かどうか
    /// </summary>
    public static bool IsPersistentChild(string name)
    {
        return name.StartsWith("Keep_");
    }

    /// <summary>
    /// 地面1枚分の飾りを置く（地面を作るたびに呼ばれる。古い飾りは GroundAutomatically が消す）
    /// </summary>
    public void Decorate(GroundAutomatically chunk)
    {
        float width = chunk.ChunkWidth;
        float length = chunk.ChunkLength;

        CreateSideGround(chunk, SideGroundLeft, -SideWidth * 0.5f, length);
        CreateSideGround(chunk, SideGroundRight, width + SideWidth * 0.5f, length);

        if (DecorPrefabs == null || DecorPrefabs.Length == 0) return;

        for (int i = 0; i < DecorPerSide; i++)
        {
            PlaceDecor(chunk, Random.Range(-SideWidth, -RoadMargin), length);
            PlaceDecor(chunk, Random.Range(width + RoadMargin, width + SideWidth), length);
        }
    }

    private void CreateSideGround(GroundAutomatically chunk, string name, float centerX, float length)
    {
        Transform t = chunk.transform.Find(name);
        if (!t)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            Destroy(plane.GetComponent<Collider>());
            plane.transform.SetParent(chunk.transform, false);

            MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
            if (chunk.groundMaterial) renderer.sharedMaterial = chunk.groundMaterial;

            t = plane.transform;
        }

        //Plane は 10m x 10m
        t.localPosition = new Vector3(centerX, SideGroundY, length * 0.5f);
        t.localScale = new Vector3(SideWidth / 10f, 1f, length / 10f);
    }

    private void PlaceDecor(GroundAutomatically chunk, float x, float length)
    {
        GameObject prefab = DecorPrefabs[Random.Range(0, DecorPrefabs.Length)];
        if (!prefab) return;

        GameObject decor = Instantiate(prefab, chunk.transform);
        decor.name = "Decor";

        float z = Random.Range(0f, length);
        decor.transform.localPosition = new Vector3(x, SideGroundY - Random.Range(0f, MaxSink), z);
        decor.transform.localRotation = Quaternion.Euler(Random.Range(-MaxTilt, MaxTilt), Random.Range(0f, 360f), Random.Range(-MaxTilt, MaxTilt));
        decor.transform.localScale *= Random.Range(ScaleRange.x, ScaleRange.y);

        //飾りなので当たり判定は消す
        foreach (Collider col in decor.GetComponentsInChildren<Collider>(true))
        {
            Destroy(col);
        }
        foreach (Transform child in decor.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = 0;
        }
    }
}
