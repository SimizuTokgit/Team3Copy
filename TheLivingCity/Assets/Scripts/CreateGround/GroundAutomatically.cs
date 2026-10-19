using UnityEngine;

public class GroundAutomatically : MonoBehaviour
{
    //本庄
    //地形を生成する

    // ===== 地形サイズ =====
    public int width;
    public int length;

    // 地形スケール
    public float scale = 4f;

    // ===== ノイズ設定 =====

    // 山
    public float noiseScale1 = 0.05f;
    public float height1 = 10f;

    // 中くらい
    public float noiseScale2 = 0.02f;
    public float height2 = 3;

    // 細かい
    public float noiseScale3 = 0.01f;
    public float height3 = 1f;

    // ===== 分岐用 =====
    public enum RouteType
    {
        Left,
        Right
    }
    public RouteType type;

    public GenerateObstacle.Difficulty currentDifficulty;

    // ===== その他 =====
    public Material groundMaterial;

    public int chunkX;
    public int chunkZ;

    private Mesh mesh;

    public GenerateObstacle _generateObstacle;
    public ItemGenerate _itemGenerate;
    public GeneretoGround _ground;

    //プレイヤーのスタート位置
    public Vector3 PlayerStartPos;

    private float SmallPosY;

    //清水 必ず通れる道
    [Header("必ず通れる道")]
    [SerializeField] private float CorridorWidth = 9f;   //道の幅
    [SerializeField] private float CorridorDrift = 16f;  //地面1枚で道が横にずれてよい量
    [SerializeField] private float CorridorMargin = 3f;  //壁からどれだけ離すか
    [SerializeField, Range(0f, 1f)] private float ItemOnCorridorRate = 0.6f; //アイテムを道の上に置く割合
    private ChunkLayout _layout;

    //清水 たまに置くジャンプ台（Highway を傾ける）
    [Header("ジャンプ台")]
    [SerializeField] private GameObject RampPrefab;
    [SerializeField, Range(0f, 1f)] private float RampChance = 0.15f;  //地面1枚あたりに置く確率
    [SerializeField] private float RampAngle = 12f;
    [SerializeField] private Color RampOutlineColor = new Color(1f, 0.85f, 0.2f);

    private void Awake()
    {
        _ground = FindAnyObjectByType<GeneretoGround>();
    }

    // ===============================
    // ルートごとの設定
    // ===============================
    public void ApplyRouteSettings(RouteType route)
    {
        switch (route)
        {
            case RouteType.Left: // 安全ルート
                currentDifficulty = GenerateObstacle.Difficulty.Easy;
                break;

            case RouteType.Right: // 危険
                currentDifficulty = GenerateObstacle.Difficulty.Hard;
                break;
        }
    }

    // ===============================
    // 外部から生成
    // ===============================
    public void GenerateGround(int cX, int cZ, float seedX, float seedZ, bool Instance)
    {
        chunkX = cX;
        chunkZ = cZ;

        ApplyRouteSettings(type);

        CreateMesh(seedX, seedZ, Instance);
    }

    /// <summary>
    /// メッシュ生成
    /// </summary>
    void CreateMesh(float seedX, float seedZ, bool Instance)
    {
        SmallPosY = float.PositiveInfinity;

        //清水 前に置いた障害物はこの下でまとめて消すので、覚えているリストを空にする
        if (_generateObstacle) _generateObstacle.ForgetSpawned();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name == "LeftWall" || transform.GetChild(i).name == "RightWall")
                continue;

            //清水 道の外の地面は使い回す
            if (MapDecoration.IsPersistentChild(transform.GetChild(i).name))
                continue;

            //清水 Destroy はフレームの最後なので、先に非表示にして、この後の障害物チェックに数えないようにする
            transform.GetChild(i).gameObject.SetActive(false);
            Destroy(transform.GetChild(i).gameObject);
        }

        if (mesh == null)
            mesh = new Mesh();
        else
            mesh.Clear();


        //頂点数の計算
        int vertCount = (width + 1) * (length + 1);
        //頂点情報
        Vector3[] vertices = new Vector3[vertCount];
        //テクスチャ貼るのに必要
        Vector2[] uv = new Vector2[vertCount];

        //三角形情報
        int[] triangles = new int[width * length * 6];


        //頂点生成
        for (int z = 0; z <= length; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                //現在の頂点
                int i = x + z * (width + 1);

                //ワールド座標の計算
                float worldX = seedX + x + chunkX * width;
                float worldZ = seedZ + z + chunkZ * length;

                //基本地形の高さ
                float baseHeight =
                    Mathf.PerlinNoise(worldX * noiseScale2, worldZ * noiseScale2) * height2 +
                    Mathf.PerlinNoise(worldX * noiseScale3, worldZ * noiseScale3) * height3;

                //山生成用ノイズ
                float mountainNoise = Mathf.PerlinNoise(worldX * noiseScale1, worldZ * noiseScale1);

                //山の高さ
                float mountainHeight = 0;

                //一定値以上なら山を生成
                if (mountainNoise > 0.25f)
                {
                    //山を滑らかに
                    float m = Mathf.InverseLerp(0.25f, 0.85f, mountainNoise);
                    mountainHeight = Mathf.SmoothStep(0f, height1, m);
                }

                //最終的な高さ
                float y = baseHeight + mountainHeight;

                //頂点座標を設定
                vertices[i] = new Vector3(x * scale, y, z * scale);
                uv[i] = new Vector2((float)x / width, (float)z / length);

                if (SmallPosY > y)
                    SmallPosY = y;
            }
        }

        int t = 0;

        //三角形の生成
        //四角形を2つの三角形に分ける
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                //四角形の左下頂点
                int i = x + z * (width + 1);

                //三角形１
                //左下
                triangles[t++] = i;
                //左上
                triangles[t++] = i + width + 1;
                //右下
                triangles[t++] = i + 1;

                //三角形２
                //右下
                triangles[t++] = i + 1;
                //左上
                triangles[t++] = i + width + 1;
                //右上
                triangles[t++] = i + width + 2;
            }
        }

        //メッシュに反映
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        //法線を自動計算
        mesh.RecalculateNormals();

        // ===== MeshFilter =====
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter == null)
            filter = gameObject.AddComponent<MeshFilter>();

        filter.sharedMesh = mesh;

        // ===== MeshRenderer =====
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<MeshRenderer>();

        //マテリアルの当てはめ
        if (groundMaterial != null)
            renderer.sharedMaterial = groundMaterial;

        // ===== MeshCollider =====
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<MeshCollider>();

        //Coliderの更新
        collider.sharedMesh = null;
        collider.sharedMesh = mesh;

        //外に出ないように見えない壁を生成
        CreateSideWalls();

        //清水 行けないことが分かるように、端に半透明の壁を見せる
        BuildBoundaryWalls(vertices);

        //清水 道の外の街並み
        if (_ground != null && _ground.Decoration != null)
            _ground.Decoration.Decorate(this);

        //アイテムと障害物の生成
        if (_generateObstacle != null && Instance)
        {
            if (!_ground._isBoss)
                GenObsItem();
            //清水 ストーリーのボス戦はボスごとのギミックを置く
            else if (_ground.BossBattle != null)
            {
                //清水 ボス戦でもふつうの障害物は置く（ボスの手前は空ける）
                if (_ground.BossBattle.AllowObstacles(this)) GenObsItem();
                _ground.BossBattle.SpawnHazards(this);
            }
            else
                GenTentacles();
        }

        if (chunkX == 0 && chunkZ == 0)
        {
            int x = width / 2;
            int z = 1;

            int index = x + z * (width + 1);

            PlayerStartPos = mesh.vertices[index];
        }
    }

    /// <summary>
    /// アイテム、障害物生成
    /// </summary>
    public void GenObsItem()
    {
        _generateObstacle.Generate(mesh, width, length, currentDifficulty, SmallPosY);

        CheckScript();

        if (_generateObstacle.Connectobj())
        {
            _ground.ConnectStart();

            _ground.GroundNumberminus();
        }
        else
        {
            _generateObstacle.Connectend();
            Connectend();

            _ground.ConnectEnd();
        }

        //清水 細い障害物を飾りにして、必ず通れる道を空けてから、取れる場所にアイテムを置く
        _layout = new ChunkLayout(this);
        _layout.Collect();
        _layout.ClearCorridor(_ground.GetCorridorStart(chunkZ, ChunkWidth * 0.5f), CorridorWidth, CorridorDrift, CorridorMargin);
        _ground.SetCorridorEnd(chunkZ, _layout.CorridorEndX);
        TryPlaceRamp();
        _generateObstacle.ApplyOutlines();

        _itemGenerate.ItemGenReachable(this, _layout, ItemOnCorridorRate);
    }

    public void GenTentacles()
    {
        _generateObstacle.TentaclesGenerate(mesh, width, length, 1, SmallPosY);

        CheckScript();

        if (_generateObstacle.Connectobj())
        {
            _ground.ConnectStart();

            _ground.GroundNumberminus();
        }
        else
        {
            _generateObstacle.Connectend();
            Connectend();

            _ground.ConnectEnd();
        }
    }

    /// <summary>
    /// アイテムの出現率リセット
    /// </summary>
    public void ItemRateReset()
    {
        _itemGenerate.ResetAllWeight();
    }

    /// <summary>
    /// アイテムの出現率変更
    /// </summary>
    public void ItemRateChange(string ItemName,int ItemRate)
    {
        _itemGenerate.SetItemWeight(ItemName, ItemRate);
    }

    public void Connectend()
    {
        CheckScript();

        _generateObstacle.Connectend();
    }

    public void ConnectStart()
    {
        CheckScript();

        _generateObstacle.ConnectStart(_ground.SameGroundCount);
    }

    /// <summary>
    /// 清水 この地面に置いた障害物とアイテムを消す（ジャンプ台のまわりを空けるときなど）
    /// </summary>
    public void ClearObstaclesAndItems()
    {
        if (_generateObstacle) _generateObstacle.DestroySpawned();
        foreach (Item item in GetComponentsInChildren<Item>())
        {
            Destroy(item.gameObject);
        }
    }

    //清水 チャンクの大きさ（ローカル座標）
    public float ChunkWidth => width * scale;
    public float ChunkLength => length * scale;

    /// <summary>
    /// チャンク内のローカル座標から地面の高さを返す
    /// </summary>
    public float GetLocalHeight(float localX, float localZ)
    {
        if (mesh == null) return 0f;

        int x = Mathf.Clamp(Mathf.RoundToInt(localX / scale), 0, width);
        int z = Mathf.Clamp(Mathf.RoundToInt(localZ / scale), 0, length);

        return mesh.vertices[x + z * (width + 1)].y;
    }

    /// <summary>
    /// 清水 置いた障害物を地面の高さに合わせる
    /// 今までは高さ0に置いていたので、山の中に埋まった見えない障害物に当たっていた
    /// 子がいれば子ごとに、その場所の地面の高さだけ持ち上げる
    /// </summary>
    public void SnapToGround(Transform obj)
    {
        if (obj.childCount == 0)
        {
            SnapOne(obj);
            return;
        }

        foreach (Transform child in obj)
        {
            SnapOne(child);
        }
    }

    private void SnapOne(Transform t)
    {
        Vector3 local = transform.InverseTransformPoint(t.position);
        t.position += transform.up * GetLocalHeight(local.x, local.z);
    }

    //清水 たまに、必ず通れる道の上にジャンプ台を置く（まわりの障害物はどける）
    private void TryPlaceRamp()
    {
        if (!RampPrefab || Random.value >= RampChance) return;

        const float rampWidth = 18f;
        const float rampLength = 50f;
        float length = rampLength * Mathf.Cos(RampAngle * Mathf.Deg2Rad);

        float startZ = Random.Range(ChunkLength * 0.08f, ChunkLength - length - ChunkLength * 0.08f);
        if (startZ < 0f) return;

        //道の真ん中に合わせる（壁にはみ出さないように）
        float half = rampWidth * 0.5f + 1f;
        float x = Mathf.Clamp(_layout.CorridorX(startZ + length * 0.5f), half, ChunkWidth - half);

        //ジャンプ台の下と、飛び出した先を少し空ける
        _layout.ClearArea(new Rect(x - half, startZ - 2f, half * 2f, length + 20f));

        Vector3 start = transform.TransformPoint(new Vector3(x, GetLocalHeight(x, startZ), startZ));
        BossRamp ramp = BossRamp.Create(RampPrefab, start, RampAngle, null, RampOutlineColor);
        ramp.transform.SetParent(transform, true);
    }

    //清水 左右の端に、地形に沿った半透明の壁を作る
    private void BuildBoundaryWalls(Vector3[] vertices)
    {
        float[] left = new float[length + 1];
        float[] right = new float[length + 1];
        for (int z = 0; z <= length; z++)
        {
            left[z] = vertices[z * (width + 1)].y;
            right[z] = vertices[width + z * (width + 1)].y;
        }

        BoundaryWallVisual.Build(transform, "Keep_WallVisualL", 0f, left, scale);
        BoundaryWallVisual.Build(transform, "Keep_WallVisualR", width * scale, right, scale);
    }

    private void CheckScript()
    {
        if (_ground == null)
            _ground = FindAnyObjectByType<GeneretoGround>();
    }

    private void CreateWall(string name, Vector3 center, Vector3 size)
    {
        Transform wall = transform.Find(name);

        GameObject obj;

        if (wall == null)
        {
            obj = new GameObject(name);
            obj.transform.parent = transform;
        }
        else
        {
            obj = wall.gameObject;
        }

        obj.transform.localPosition = center;

        BoxCollider col = obj.GetComponent<BoxCollider>();

        if (col == null)
            col = obj.AddComponent<BoxCollider>();

        col.size = size;
    }

    private void CreateSideWalls()
    {
        float wallHeight = 100f;
        float wallThickness = 5f;

        float mapWidth = width * scale;
        float mapLength = length * scale;

        //左壁
        CreateWall("LeftWall",
            new Vector3(-wallThickness * 0.5f, wallHeight * 0.5f, mapLength * 0.5f),
            new Vector3(wallThickness, wallHeight, mapLength));

        //右壁
        CreateWall("RightWall",
            new Vector3(mapWidth + wallThickness * 0.5f, wallHeight * 0.5f, mapLength * 0.5f),
            new Vector3(wallThickness, wallHeight, mapLength));
    }
}
