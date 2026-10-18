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

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name == "LeftWall" || transform.GetChild(i).name == "RightWall")
                continue;

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

        //アイテムと障害物の生成
        if (_generateObstacle != null && Instance)
        {
            if (!_ground._isBoss)
                GenObsItem();
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

        _itemGenerate.ItemGen(mesh, width, length);
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
