using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

//清水
//ストーリーモードのボス戦を順番に進める
//一定距離走るたびにボスが出て、3回どつくと倒せる。最後のマザーコンピューターを倒したらクリア
public class BossBattleManager : MonoBehaviour
{
    public enum BossAttack
    {
        TentacleSwing,    //1 触手を振って攻撃
        ElectricTentacle, //2 感電する触手が巻き付いたオブジェクト（一撃死）
        GasBurner,        //3 ガスバーナー（一撃死）
        Train,            //4 走ってくる電車（一撃死）
        All               //5 今までの攻撃全部
    }

    [System.Serializable]
    public class BossEncounter
    {
        public string BossName;
        public GameObject BossPrefab;
        public BossAttack Attack;
        public float RunDistance = 2000f;  //前のボスを倒してから（1体目はスタートから）出てくるまでの距離
        public int HazardsPerChunk = 1;    //地面1枚につき出すギミックの数
    }

    [Header("ボスの順番")]
    [SerializeField] private List<BossEncounter> Encounters = new List<BossEncounter>();

    [Header("ボスの出し方")]
    [SerializeField] private float BossSpawnAhead = 1000f;    //プレイヤーの何m先に出すか
    [SerializeField] private float BossSpawnX = 24f;
    [SerializeField] private float BossSpawnY = 20f;
    [SerializeField] private float TimeBonusOnDefeat = 30f;  //ボスを倒したら回復する残り時間
    [SerializeField] private float SafeDistanceOnStart = 200f; //ボス戦開始時、この距離より先の地面からギミックを置く
    [SerializeField] private GameObject CutSceneDirectorObject; //ボス登場のカットシーン
    [SerializeField] private GameObject DefeatEffect;         //倒したときのエフェクト（無くてもよい）
    [SerializeField] private AudioClip DefeatSE;

    [Header("2 感電触手")]
    [SerializeField] private GameObject[] ElectricBasePrefabs; //触手が巻き付く土台（電柱など）
    [SerializeField] private GameObject TentaclePrefab;
    [SerializeField] private float TentacleScale = 0.5f;

    [Header("3 ガスバーナー")]
    [SerializeField] private GameObject BurnerPrefab;
    [SerializeField] private float BurnerSpacing = 4f;

    [Header("4 電車")]
    [SerializeField] private GameObject TrainPrefab;
    [SerializeField] private float TrainModelYaw = 0f;          //モデルの正面がずれていたらここで合わせる
    [SerializeField] private float CrossingTrainSpeed = 30f;
    [SerializeField] private float CrossingTriggerDistance = 130f;
    [SerializeField] private float OncomingTrainSpeed = 35f;
    [SerializeField] private float OncomingTriggerDistance = 260f;
    [SerializeField] private AudioClip TrainWarningSE;

    private GameManager _gameManager;
    private GeneretoGround _ground;
    private TimeManager _timeManager;
    private Transform _player;

    private int _index = 0;
    private float _nextBossZ;
    private bool _isStarted = false;
    private MotherComputer _currentBoss;

    public bool InBattle { get; private set; } = false;

    private BossEncounter Current => Encounters[_index];
    private bool IsLastBoss => _index >= Encounters.Count - 1;

    private void Start()
    {
        _gameManager = FindAnyObjectByType<GameManager>();
        _ground = FindAnyObjectByType<GeneretoGround>();
        _timeManager = FindAnyObjectByType<TimeManager>();
        _player = FindAnyObjectByType<Player>().transform;
    }

    private void Update()
    {
        if (_gameManager._currentState != GameManager.GameState.Playing) return;
        if (Encounters.Count == 0) return;

        //カウントダウンが終わってから距離を数えはじめる
        if (!_isStarted)
        {
            _isStarted = true;
            _nextBossZ = _player.position.z + Current.RunDistance;
        }

        if (!InBattle && _currentBoss == null && _index < Encounters.Count && _player.position.z >= _nextBossZ)
        {
            StartBattle();
        }
    }

    /// <summary>
    /// ボス戦開始
    /// </summary>
    private void StartBattle()
    {
        InBattle = true;

        //ふつうの障害物とアイテムを消して、ここからはボスのギミックを出す
        _ground._isBoss = true;
        _ground.meshInfo._generateObstacle.DeleteAllObstacles();
        _ground.meshInfo._itemGenerate.DeleteAllItems();

        Vector3 pos = new Vector3(BossSpawnX, BossSpawnY, _player.position.z + BossSpawnAhead);
        GameObject boss = Instantiate(Current.BossPrefab, pos, Current.BossPrefab.transform.rotation);
        _currentBoss = boss.GetComponent<MotherComputer>();
        _currentBoss.SetBattleManager(this);

        //もうできている先の地面にもギミックを置く（すぐ近くは避ける）
        foreach (GroundAutomatically chunk in _ground.groundScripts)
        {
            if (chunk.gameObject.activeInHierarchy && chunk.transform.position.z > _player.position.z + SafeDistanceOnStart)
            {
                SpawnHazards(chunk);
            }
        }

        PlayCutScene(boss.transform);
    }

    private void PlayCutScene(Transform boss)
    {
        if (!CutSceneDirectorObject) return;

        CutSceneDirectorObject.SetActive(true);

        CineCameraController cine = CutSceneDirectorObject.GetComponent<CineCameraController>();
        if (cine) cine.SetTarget(boss);

        //2体目からは止まっているので最初から流し直す
        PlayableDirector director = CutSceneDirectorObject.GetComponent<PlayableDirector>();
        if (director)
        {
            director.time = 0;
            director.Play();
        }
    }

    /// <summary>
    /// ボスを倒した（MotherComputer から呼ばれる）
    /// </summary>
    public void OnBossDefeated(MotherComputer boss)
    {
        if (DefeatEffect) Instantiate(DefeatEffect, boss.transform.position, Quaternion.identity);
        if (DefeatSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(DefeatSE);

        if (IsLastBoss)
        {
            //マザーコンピューターを倒した。世界を救った
            _gameManager.SetState(GameManager.GameState.GameClear);
            return;
        }

        Destroy(boss.gameObject);
        _currentBoss = null;

        //ボスのギミックを片付けて、ふつうのランに戻す
        InBattle = false;
        _ground._isBoss = false;
        _ground.ConnectEnd();
        BossHazard.DestroyAll();

        if (_timeManager) _timeManager.AddTime(TimeBonusOnDefeat);

        _index++;
        _nextBossZ = _player.position.z + Current.RunDistance;
    }

    /// <summary>
    /// ボス戦中に新しい地面ができたらギミックを置く（GroundAutomatically から呼ばれる）
    /// </summary>
    public void SpawnHazards(GroundAutomatically chunk)
    {
        if (!InBattle) return;

        for (int i = 0; i < Current.HazardsPerChunk; i++)
        {
            BossAttack attack = Current.Attack;
            if (attack == BossAttack.All) attack = (BossAttack)Random.Range(0, (int)BossAttack.All);

            switch (attack)
            {
                case BossAttack.TentacleSwing: SpawnTentacleSwing(chunk); break;
                case BossAttack.ElectricTentacle: SpawnElectricTentacle(chunk); break;
                case BossAttack.GasBurner: SpawnBurner(chunk); break;
                case BossAttack.Train: SpawnTrain(chunk); break;
            }
        }
    }

    //チャンク内のランダムな位置（ローカル座標、高さは地面に合わせる）
    private Vector3 RandomPoint(GroundAutomatically chunk, float marginX)
    {
        float x = Random.Range(marginX, chunk.ChunkWidth - marginX);
        float z = Random.Range(chunk.ChunkLength * 0.2f, chunk.ChunkLength * 0.8f);
        return new Vector3(x, chunk.GetLocalHeight(x, z), z);
    }

    private GameObject CreateHazardRoot(GroundAutomatically chunk, string name, Vector3 localPos)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(chunk.transform, false);
        root.transform.localPosition = localPos;
        root.AddComponent<BossHazard>();
        return root;
    }

    // 1 触手振り：今までのボス戦と同じ触手マップを置く
    private void SpawnTentacleSwing(GroundAutomatically chunk)
    {
        HashSet<Transform> before = new HashSet<Transform>();
        foreach (Transform child in chunk.transform) before.Add(child);

        chunk.GenTentacles();

        foreach (Transform child in chunk.transform)
        {
            if (!before.Contains(child) && !child.GetComponent<BossHazard>()) child.gameObject.AddComponent<BossHazard>();
        }
    }

    // 2 感電触手：電柱などに触手を巻き付けて電気を流す
    private void SpawnElectricTentacle(GroundAutomatically chunk)
    {
        if (ElectricBasePrefabs == null || ElectricBasePrefabs.Length == 0) return;

        GameObject root = CreateHazardRoot(chunk, "ElectricTentacle", RandomPoint(chunk, 6f));

        GameObject basePrefab = ElectricBasePrefabs[Random.Range(0, ElectricBasePrefabs.Length)];
        Instantiate(basePrefab, root.transform).transform.localPosition = Vector3.zero;

        if (TentaclePrefab)
        {
            GameObject tentacle = Instantiate(TentaclePrefab, root.transform);
            tentacle.transform.localPosition = Vector3.zero;
            tentacle.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            tentacle.transform.localScale *= TentacleScale;
        }

        root.AddComponent<ElectricHazard>();
    }

    // 3 ガスバーナー：道幅いっぱいに並べる
    private void SpawnBurner(GroundAutomatically chunk)
    {
        if (!BurnerPrefab) return;

        Vector3 pos = RandomPoint(chunk, 0f);
        pos.x = 0f;

        GameObject root = CreateHazardRoot(chunk, "BurnerRow", pos);
        BurnerHazard burner = root.AddComponent<BurnerHazard>();

        BurnerHazard.Pattern pattern = Random.value < 0.5f ? BurnerHazard.Pattern.AllAtOnce : BurnerHazard.Pattern.Alternate;
        burner.Setup(BurnerPrefab, chunk.ChunkWidth, BurnerSpacing, pattern, Random.Range(0f, 3f));
    }

    // 4 電車：横切るか正面から来る
    private void SpawnTrain(GroundAutomatically chunk)
    {
        if (!TrainPrefab) return;

        bool crossing = Random.value < 0.5f;
        Vector3 pos = RandomPoint(chunk, 8f);
        Vector3 dir;

        if (crossing)
        {
            //道の外から反対側へ横切る
            bool fromLeft = Random.value < 0.5f;
            pos.x = fromLeft ? -30f : chunk.ChunkWidth + 30f;
            dir = fromLeft ? Vector3.right : Vector3.left;
        }
        else
        {
            dir = Vector3.back;
        }

        GameObject root = CreateHazardRoot(chunk, "Train", pos);
        root.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, TrainModelYaw, 0);
        Instantiate(TrainPrefab, root.transform).transform.localPosition = Vector3.zero;

        TrainHazard train = root.AddComponent<TrainHazard>();
        if (crossing)
            train.Setup(TrainHazard.Route.Crossing, dir, CrossingTrainSpeed, CrossingTriggerDistance, chunk.ChunkWidth + 60f, TrainWarningSE);
        else
            train.Setup(TrainHazard.Route.Oncoming, dir, OncomingTrainSpeed, OncomingTriggerDistance, 2000f, TrainWarningSE);
    }
}
