using System.Collections.Generic;
using UnityEngine;

public class GeneretoGround : MonoBehaviour
{
    //本庄
    //地形を生成する
    //古い地形を消して新しい地形を生成する

     
    public GameObject groundPrefab;
    public Transform player;

    private float seedX;
    private float seedZ;

    public int Maxz;
    public int Maxx;


    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();

    private Queue<GameObject> pool = new Queue<GameObject>();

    //分岐区間かどうか
    public bool Branches = false;

    //分岐区間に表示するUI
    public GameObject BranchesUI;

    //分岐区間の秒数
    public float BranchesTime;
    private float _branchesTime;

    //分岐区間始まったz座標
    private float brancheStartPos;

    //プレイヤーが前いたチャンク
    private int currentPlayerChunkZ;
    //プレイヤーが今いるチャンク
    private int playerChunkZ;
    //1チャンクのz座標の大きさ
    private float chunkLength;


    public GroundAutomatically meshInfo;

    //生成した地面が入っている
    public List<GameObject> ground = new List<GameObject>();

    public List<GroundAutomatically> groundScripts = new List<GroundAutomatically>();

    private CheckPointInstance checkPointInstance;

    //プレイヤーのスタート位置
    private Vector3 PlayerStartPosition;

    //ブランチどちらの難易度になるか分けるx座標
    private float BrancheCangeX;

    private ItemUI itemUI;

    //どのくらい同じ地形が続くか
    public int SameGroundCount;

    //ボス戦かどうか
    public bool _isBoss = false;

    //清水 ストーリーのボス戦を進めるマネージャー（エンドレスにはいない）
    public BossBattleManager BossBattle { get; private set; }

    //清水 道の外の飾り（無くてもよい）
    public MapDecoration Decoration { get; private set; }

    //清水 地面ごとの「必ず通れる道」の終わりのx。次の地面はここから道を始める
    private readonly Dictionary<int, float> corridorEnds = new Dictionary<int, float>();

    public float GetCorridorStart(int chunkZ, float defaultX)
    {
        return corridorEnds.TryGetValue(chunkZ - 1, out float x) ? x : defaultX;
    }

    public void SetCorridorEnd(int chunkZ, float x)
    {
        corridorEnds[chunkZ] = x;
        corridorEnds.Remove(chunkZ - Maxz - 2); //古いものは捨てる
    }

    void Start()
    {
        BossBattle = FindAnyObjectByType<BossBattleManager>();
        Decoration = FindAnyObjectByType<MapDecoration>();

        SameGroundCount = -2;
        BranchesUI.SetActive(false);

        PlayerStartPosition = new Vector3(0.1f, 0, 3);

        seedX = Random.value * 1000f;
        seedZ = Random.value * 1000f;

        Branches = false;
        _branchesTime = BranchesTime;

        meshInfo = groundPrefab.GetComponent<GroundAutomatically>();

        //最初はどのアイテムも同じ確率で出る
        meshInfo.ItemRateReset();

        checkPointInstance = FindAnyObjectByType<CheckPointInstance>();

        chunkLength = meshInfo.length * meshInfo.scale;

        int total = (Maxz + 1) * Maxx;

        for (int i = 0; i < total; i++)
        {
            GameObject g = Instantiate(groundPrefab);
            g.SetActive(false);
            pool.Enqueue(g);

            ground.Add(g);
            groundScripts.Add(g.GetComponent<GroundAutomatically>());
        }

        for (int z = 0; z < Maxz; z++)
        {
            for (int x = 0; x <= Maxx - 1; x++)
            {
                if (z <= 1)
                    CreateChunk(x, z, false);
                else
                    CreateChunk(x, z, true);
            }
        }

        PlayerStartPosition = groundScripts[0].PlayerStartPos;

        player.transform.position = PlayerStartPosition;
        BrancheCangeX = PlayerStartPosition.x;

        itemUI = FindAnyObjectByType<ItemUI>();
    }

    void Update()
    {
        playerChunkZ = Mathf.FloorToInt(player.position.z / chunkLength) - 1;

        InBranch();

        if(_isBoss)
        {
            if (playerChunkZ > currentPlayerChunkZ)
            {
                currentPlayerChunkZ = playerChunkZ;

                int newZ = playerChunkZ + Maxz - 1;

                for (int x = 0; x <= Maxx - 1; x++)
                {
                    CreateChunk(x, newZ, true);
                }

                int removeZ = playerChunkZ - 1;

                for (int x = 0; x <= Maxx - 1; x++)
                {
                    Vector2Int key = new Vector2Int(x, removeZ);

                    if (chunks.ContainsKey(key))
                    {
                        GameObject g = chunks[key];

                        g.SetActive(false);
                        pool.Enqueue(g);

                        chunks.Remove(key);
                    }
                }
            }
        }
        else
        {
            if (playerChunkZ > currentPlayerChunkZ)
            {
                currentPlayerChunkZ = playerChunkZ;

                int newZ = playerChunkZ + Maxz - 1;

                ////分岐区間
                //if (Branches)
                //{
                //    for (int x = 0; x <= Maxx - 1; x++)
                //    {
                //        CreateChunk(x, newZ, true);
                //    }
                //}
                ////通常地形生成
                //else
                //{
                //    for (int x = 0; x <= Maxx - 1; x++)
                //    {
                //        CreateChunk(x, newZ, true);
                //    }
                //}

                for (int x = 0; x <= Maxx - 1; x++)
                {
                    CreateChunk(x, newZ, true);
                }


                int removeZ = playerChunkZ - 1;

                for (int x = 0; x <= Maxx - 1; x++)
                {
                    Vector2Int key = new Vector2Int(x, removeZ);

                    if (chunks.ContainsKey(key))
                    {
                        GameObject g = chunks[key];

                        g.SetActive(false);
                        pool.Enqueue(g);

                        chunks.Remove(key);
                    }
                }
            }
        }
    }

    public void PlayerPosGet()
    {
        brancheStartPos = player.transform.position.z;
    }

    /// <summary>
    /// 地形生成
    /// </summary>
    private void CreateChunk(int x, int z, bool Instance)
    {
        Vector2Int key = new Vector2Int(x, z);

        if (chunks.ContainsKey(key)) return;

        GameObject g = (pool.Count > 0) ? pool.Dequeue() : Instantiate(groundPrefab);

        g.layer = 8;
        g.SetActive(true);

        GroundAutomatically mesh = g.GetComponent<GroundAutomatically>();

        mesh.GenerateGround(x, z, seedX, seedZ, Instance);

        g.transform.position = new Vector3(
            x * mesh.width * mesh.scale,
            0,
            z * mesh.length * mesh.scale
        );

        chunks.Add(key, g);
    }

    /// <summary>
    /// 地形分岐時の処理
    /// </summary>
    private void InBranch()
    {
        if (Branches)
        {
            _branchesTime -= Time.deltaTime;

            if (_branchesTime <= 0.0f)
            {
                BranchesUI.SetActive(false);

                //清水 ボス戦中は道を選ばない（いつも普通の難しさ）。選ぶのはふつうの道だけ
                if (!_isBoss)
                {
                    //難易度変更
                    ChangeDifficult();

                    //清水 もうできている先の地面にもすぐ反映する（今までは768m先からしか変わらなかった）
                    RegenerateAhead();
                }

                //新しくチェックポイントを作る
                checkPointInstance.InstanceCheckPoints(player.transform.position.z - brancheStartPos);

                Branches = false;

                _branchesTime = BranchesTime;
            }
        }
    }

    //清水 分岐で選んだ難易度とアイテムで、プレイヤーの少し先から地面の中身を作り直す
    [SerializeField] private float RegenerateSafeDistance = 120f; //すぐ目の前は作り直さない

    private void RegenerateAhead()
    {
        bool hard = player.transform.position.x <= BrancheCangeX;
        GameHUD.Get().Banner(hard ? "HARD ROUTE" : "EASY ROUTE", hard ? GameHUD.Danger : GameHUD.Good, 1.4f);

        //ボス戦中はギミックと混ざるので作り直さない
        if (_isBoss) return;

        //必ず通れる道が前の地面からつながるように、手前から順に作り直す
        List<GroundAutomatically> ordered = new List<GroundAutomatically>(groundScripts);
        ordered.Sort((a, b) => a.chunkZ.CompareTo(b.chunkZ));

        foreach (GroundAutomatically chunk in ordered)
        {
            if (!chunk.gameObject.activeInHierarchy) continue;
            if (chunk.chunkZ <= 1) continue; //スタート地点
            if (chunk.transform.position.z < player.position.z + RegenerateSafeDistance) continue;

            chunk.ClearObstaclesAndItems();
            chunk.GenObsItem();
        }
    }

    public void BranchStartSet()
    {
        //清水 ボス戦中は道を選ばないので、分かれ道の案内も出さない
        BranchesUI.SetActive(!_isBoss);
        GroundNumberReset();

        //for (int i = 0; i < ground.Count; i++)
        //{
        //    foreach (Transform child in ground[i].transform)
        //    {
        //        if (child.name == "LeftWall" || child.name == "RightWall")
        //            continue;

        //        Destroy(child.gameObject);
        //    }
        //}
    }

    /// <summary>
    /// 難易度変更
    /// </summary>
    public void ChangeDifficult()
    {
        if (player.transform.position.x <= BrancheCangeX)
        {
            for (int i = 0; i < ground.Count; i++)
            {
                GroundAutomatically grounds = groundScripts[i];

                grounds.ItemRateReset();

                grounds.ItemRateChange(itemUI.LeftItemName1(), 10);
                grounds.ItemRateChange(itemUI.LeftItemName2(), 10);

                grounds.type = GroundAutomatically.RouteType.Right;

                grounds.ApplyRouteSettings(grounds.type);

                grounds.Connectend();

                //清水 return だと1枚目の地面しか難易度が変わらなかったので continue にした
                //BossBattleManager がいるときは地面ができたときにギミックを置くのでここでは置かない
                if (grounds.chunkZ > playerChunkZ + 1)
                {
                    if (!_isBoss)
                        continue;
                    //grounds.GenObsItem();
                    else if (BossBattle == null)
                        grounds.GenTentacles();

                }
            }
            Debug.Log("Hard");
        }
        else
        {
            for (int i = 0; i < ground.Count; i++)
            {
                GroundAutomatically grounds = groundScripts[i];

                grounds.ItemRateReset();

                grounds.ItemRateChange(itemUI.RightItemName(), 10);

                grounds.type = GroundAutomatically.RouteType.Left;

                grounds.ApplyRouteSettings(grounds.type);

                grounds.Connectend();

                //清水 return だと1枚目の地面しか難易度が変わらなかったので continue にした
                //BossBattleManager がいるときは地面ができたときにギミックを置くのでここでは置かない
                if (grounds.chunkZ > playerChunkZ + 1)
                {
                    if (!_isBoss)
                        continue;
                    //grounds.GenObsItem();
                    else if (BossBattle == null)
                        grounds.GenTentacles();
                }
            }
            Debug.Log("Easy");
        }
    }

    public void ConnectStart()
    {
        for (int i = 0; i < ground.Count; i++)
        {
            if (SameGroundCount == -2)
                SameGroundCount = Random.Range(3, 7);

            groundScripts[i].ConnectStart();
        }
    }

    public void ConnectEnd()
    {
        for (int i = 0; i < ground.Count; i++)
        {
            groundScripts[i].Connectend();
        }

        GroundNumberReset();
    }

    public void GroundNumberReset()
    {
        SameGroundCount = -2;
    }

    public void GroundNumberminus()
    {
        SameGroundCount--;
    }
}
