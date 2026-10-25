using System.Collections.Generic;
using UnityEngine;

public class GenerateObstacle : MonoBehaviour
{
    //本庄
    //障害物の生成

    //障害物を入れる
    [SerializeField]
    private GameObject[] obstacles;

    //つなげたい最初の障害物
    [SerializeField]
    private GameObject ConnectStartobstacle;

    //つなげる障害物
    [SerializeField]
    private GameObject[] Connectobstacle;

    [SerializeField]
    private GameObject[] TentaclesObstacles;

    [SerializeField]
    private GameObject TentaclesConnectStartobstacle;

    [SerializeField]
    private GameObject[] TentaclesConnectobstacle;

    //次からつなげるかどうか
    public bool Connect;

    //つなげる回数
    public int ContinueObstacleNumber;

    //清水 障害物を見やすくするアウトライン（QuickOutline）
    [SerializeField] private bool UseOutline = true;
    [SerializeField] private Color ObstacleOutlineColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private float ObstacleOutlineWidth = 4f;
    [SerializeField] private Color SmallOutlineColor = new Color(1f, 0.9f, 0.35f); //細い物（HPの減りが小さい物）
    [SerializeField] private float SmallOutlineWidth = 2f;

    //清水 カタログから1つずつ並べる（マッププレハブを手で組まなくてよい）
    [Header("カタログで並べる")]
    [SerializeField] private ObstacleCatalog Catalog;
    [SerializeField, Range(0f, 1f)] private float CatalogRate = 0.7f; //地面1枚ごとにカタログで並べる確率（残りは今までのマッププレハブ）

    private GroundAutomatically _groundMesh;
    private readonly List<GameObject> _placed = new List<GameObject>();
    private readonly List<GameObject> _spawned = new List<GameObject>(); //この地面に置いた障害物（作り直すまで覚えておく）

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
    }
    public Difficulty difficulty;

    public void DeleteAllObstacles()
    {
        // 削除対象のプレハブをまとめる
        List<GameObject> prefabs = new List<GameObject>();

        prefabs.AddRange(obstacles);

        if (ConnectStartobstacle != null)
            prefabs.Add(ConnectStartobstacle);

        prefabs.AddRange(Connectobstacle);

        // シーン内の全GameObjectを取得
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in sceneObjects)
        {
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) continue;

                // Instantiateされたオブジェクトは通常 "(Clone)" が付く
                if (obj.name == prefab.name || obj.name == prefab.name + "(Clone)")
                {
                    Destroy(obj);
                    break;
                }
            }
        }
    }

    public void Generate(Mesh mesh, int width, int length, Difficulty difficulty, float posY)
    {
        if (obstacles == null || obstacles.Length == 0) return;

        int spawnCount = 0;

        switch (difficulty)
        {
            case Difficulty.Easy:
                spawnCount = 1;
                break;

            case Difficulty.Normal:
                spawnCount = 1;
                break;

            case Difficulty.Hard:
                spawnCount = 1;
                break;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            int x = width / 2;
            int z = length / 2;

            int index = x + z * (width + 1);

            if (index >= mesh.vertices.Length) continue;

            Vector3 pos = mesh.vertices[index];

            if (!Connect)
            {
                //清水 カタログがあれば、たいていはそこから1つずつ並べる
                if (Catalog && Random.value < CatalogRate)
                {
                    GenerateFromCatalog(mesh, difficulty);
                    continue;
                }

                int objNumber = Random.Range(0, obstacles.Length);

                if (obstacles[objNumber] == ConnectStartobstacle)
                    Connect = true;

                GameObject obj = Instantiate(obstacles[objNumber], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);
            }
            else
            {
                GameObject obj = Instantiate(Connectobstacle[Random.Range(0, Connectobstacle.Length)], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);

                if (ContinueObstacleNumber == 0)
                {
                    Connect = false;
                }
            }
        }
    }

    public void TentaclesGenerate(Mesh mesh, int width, int length, int spawnCount, float posY)
    {
        if (TentaclesObstacles == null || TentaclesObstacles.Length == 0) return;

        for (int i = 0; i < spawnCount; i++)
        {
            int x = width / 2;
            int z = length / 2;

            int index = x + z * (width + 1);

            if (index >= mesh.vertices.Length) continue;

            Vector3 pos = mesh.vertices[index];

            if (!Connect)
            {
                int objNumber = Random.Range(0, TentaclesObstacles.Length);

                if (TentaclesObstacles[objNumber] == TentaclesConnectStartobstacle)
                    Connect = true;

                GameObject obj = Instantiate(TentaclesObstacles[objNumber], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);
                ApplyOutlines();
            }
            else
            {
                GameObject obj = Instantiate(TentaclesConnectobstacle[Random.Range(0, TentaclesConnectobstacle.Length)], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);
                ApplyOutlines();

                if (ContinueObstacleNumber == 0)
                {
                    Connect = false;
                }
            }
        }
    }

    /// <summary>
    /// 清水 カタログの障害物を、間隔をあけてランダムに並べる
    /// 当たり判定が無いモデルには自動で付け、障害物レイヤーにする
    /// </summary>
    private void GenerateFromCatalog(Mesh mesh, Difficulty difficulty)
    {
        Bounds area = mesh.bounds;
        float minX = area.min.x + Catalog.EdgeMargin;
        float maxX = area.max.x - Catalog.EdgeMargin;
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");

        List<Vector2> used = new List<Vector2>();
        int count = Catalog.GetCount(difficulty);

        for (int i = 0; i < count; i++)
        {
            ObstacleCatalog.Entry entry = Catalog.Pick();
            if (entry == null) return;

            //ほかの障害物と近すぎない場所を探す
            Vector2 pos = Vector2.zero;
            bool ok = false;
            for (int t = 0; t < 10 && !ok; t++)
            {
                pos = new Vector2(Random.Range(minX, maxX), Random.Range(area.min.z, area.max.z));
                ok = true;
                foreach (Vector2 u in used)
                {
                    if ((u - pos).sqrMagnitude < Catalog.MinSpacing * Catalog.MinSpacing) { ok = false; break; }
                }
            }
            if (!ok) continue;
            used.Add(pos);

            GameObject obj = Instantiate(entry.Prefab, transform);
            obj.transform.localPosition = new Vector3(pos.x, entry.HeightOffset, pos.y);
            if (entry.YawStep > 0f)
            {
                int steps = Mathf.Max(1, Mathf.RoundToInt(360f / entry.YawStep));
                obj.transform.localRotation = Quaternion.Euler(0f, entry.YawStep * Random.Range(0, steps), 0f);
            }
            obj.transform.localScale = entry.Prefab.transform.localScale * Random.Range(entry.ScaleRange.x, entry.ScaleRange.y);

            //新しいモデルでもそのまま障害物になるようにする
            ModelSlot.EnsureCollider(obj, true);
            foreach (Collider col in obj.GetComponentsInChildren<Collider>()) col.isTrigger = true;
            if (obstacleLayer >= 0) ModelSlot.SetLayer(obj, obstacleLayer);

            ObstacleInfo info = obj.GetComponent<ObstacleInfo>();
            if (!info) info = obj.AddComponent<ObstacleInfo>();
            info.DamageRate = entry.DamageRate;

            Place(obj);
        }
    }

    /// <summary>
    /// 清水 地面に合わせて置き直す。アウトラインは ApplyOutlines でまとめて付ける
    /// </summary>
    private void Place(GameObject obj)
    {
        if (!_groundMesh) _groundMesh = GetComponent<GroundAutomatically>();
        if (_groundMesh) _groundMesh.SnapToGround(obj.transform);

        _placed.Add(obj);
        _spawned.Add(obj);
    }

    /// <summary>
    /// 清水 この地面に置いた障害物を消す
    /// </summary>
    public void DestroySpawned()
    {
        foreach (GameObject obj in _spawned)
        {
            if (obj) Destroy(obj);
        }
        _spawned.Clear();
    }

    /// <summary>
    /// 清水 地面を作り直すとき（子はまとめて消されるので、覚えているリストだけ空にする）
    /// </summary>
    public void ForgetSpawned()
    {
        _spawned.Clear();
    }

    /// <summary>
    /// 清水 置いた障害物のうち、当たり判定が残っている物だけにアウトラインを付ける
    /// 細い物（HPの減りが小さい物）は黄色の細い線にして区別する
    /// </summary>
    public void ApplyOutlines()
    {
        foreach (GameObject obj in _placed)
        {
            if (!obj || !UseOutline) continue;

            //カタログで置いた物（障害物1つ）か、子のないもの
            if (obj.transform.childCount == 0 || obj.GetComponent<ObstacleInfo>())
            {
                OutlineUnit(obj);
                continue;
            }

            //マッププレハブなら子が1つずつの障害物
            foreach (Transform child in obj.transform)
            {
                OutlineUnit(child.gameObject);
            }
        }
        _placed.Clear();
    }

    private void OutlineUnit(GameObject unit)
    {
        if (!HasEnabledCollider(unit)) return;

        ObstacleInfo info = unit.GetComponent<ObstacleInfo>();
        if (info && info.IsSmall) AddOutline(unit, SmallOutlineColor, SmallOutlineWidth);
        else AddOutline(unit, ObstacleOutlineColor, ObstacleOutlineWidth);
    }

    private static bool HasEnabledCollider(GameObject obj)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
        {
            if (col.enabled) return true;
        }
        return false;
    }

    public static void AddOutline(GameObject obj, Color color, float width)
    {
        Outline outline = obj.GetComponent<Outline>();
        if (!outline) outline = obj.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = color;
        outline.OutlineWidth = width;
    }

    public bool Connectobj()
    {
        return Connect;
    }

    public void Connectend()
    {
        Connect = false;
    }
    public void ConnectStart(int Number)
    {
        Connect = true;

        ContinueObstacleNumber = Number;
    }
}
