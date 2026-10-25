using System.Collections;
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
        public string DisplayName = "MID BOSS";  //HPバーに出す名前（TMPに日本語フォントが無いので英語）
        public GameObject BossPrefab;
        public BossAttack Attack;
        public float RunDistance = 2000f;  //前のボスを倒してから（1体目はスタートから）出てくるまでの距離
        public int HazardsPerChunk = 1;    //地面1枚につき出すギミックの数

        [Header("見た目の付け替え（空なら BossPrefab の見た目のまま）")]
        public GameObject ModelOverride;   //新しいボスのモデル。当たり判定・HP・動きは BossPrefab のものを使う
        public Vector3 ModelOffset;
        public Vector3 ModelEuler;
        public float ModelScale = 1f;
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

    [Header("ボスに当てたときの演出")]
    [SerializeField] private GameObject HitEffect;            //当てたときのエフェクト（爆発など）
    [SerializeField] private AudioClip HitSE;
    [SerializeField] private AudioClip WarningSE;             //ボス出現の警告音
    [SerializeField] private float ClearDelay = 2.0f;         //ラスボスを倒してからクリア画面に行くまで（実時間）
    [SerializeField] private float MinIntroTime = 2.5f;       //ボス登場で止めておく最短の秒数（カットシーンが無いとき用）

    [Header("ボスの手前のジャンプ台")]
    [SerializeField] private GameObject RampPrefab;              //Highway を傾けて坂にする
    [SerializeField] private float RampAngle = 15f;              //坂の角度
    [SerializeField] private float RampGap = 110f;               //坂の上の端からボスまでの距離
    [SerializeField] private float RampSpawnDistance = 600f;     //ボスがこの距離まで近づいたら置く
    [SerializeField] private float NoHazardBeforeBoss = 350f;    //ボスの手前この距離はギミックを置かない

    [Header("ボス戦の難しさ")]
    [SerializeField, Range(0f, 1f)] private float HazardChunkChance = 0.6f;     //地面1枚にギミックを置く確率
    [SerializeField, Range(0f, 1f)] private float ObstacleChanceInBattle = 0.5f; //地面1枚にふつうの障害物を置く確率
    [SerializeField] private Color RampOutlineColor = new Color(1f, 0.85f, 0.2f);

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
    private BossRamp _ramp;
    private float _rampForZ = float.NaN;

    public bool InBattle { get; private set; } = false;

    private BossEncounter Current => Encounters[_index];
    private bool IsLastBoss => _index >= Encounters.Count - 1;

    //HUD 用
    public int BossNumber => _index + 1;
    public int BossCount => Encounters.Count;
    public float CurrentRunDistance => _index < Encounters.Count ? Current.RunDistance : 0f;
    public float DistanceToNextBoss => _isStarted ? Mathf.Max(0f, _nextBossZ - _player.position.z) : (Encounters.Count > 0 ? Encounters[0].RunDistance : 0f);

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

        //ボスが止まっていて近づいてきたら、手前にジャンプ台を置く
        if (InBattle && _currentBoss && !_currentBoss.IsMoving)
        {
            float bossZ = _currentBoss.transform.position.z;
            if (_rampForZ != bossZ && bossZ - _player.position.z <= RampSpawnDistance) PlaceRamp(bossZ);
        }
    }

    /// <summary>
    /// 清水 ボスの手前を空けて、ボスに突撃できるジャンプ台を置く
    /// </summary>
    private void PlaceRamp(float bossZ)
    {
        if (!RampPrefab)
        {
            _rampForZ = bossZ;
            return;
        }

        float startZ = bossZ - RampGap - 50f * Mathf.Cos(RampAngle * Mathf.Deg2Rad);

        //登り口の地面の高さ（まだ地面ができていなければ次のフレームでもう一度）
        Vector3 from = new Vector3(BossSpawnX, 300f, startZ);
        if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 600f, LayerMask.GetMask("Ground"))) return;

        _rampForZ = bossZ;

        //ボスの手前のギミック・障害物・アイテムを片付ける
        BossHazard.DestroyInRange(bossZ - NoHazardBeforeBoss, bossZ + 100f);
        foreach (GroundAutomatically chunk in _ground.groundScripts)
        {
            if (chunk.gameObject.activeInHierarchy && IsNearBoss(chunk)) chunk.ClearObstaclesAndItems();
        }

        if (_ramp) Destroy(_ramp.gameObject);
        _ramp = BossRamp.Create(RampPrefab, hit.point, RampAngle, _currentBoss.transform, RampOutlineColor);
    }

    /// <summary>
    /// ボス戦開始
    /// </summary>
    private void StartBattle()
    {
        InBattle = true;
        StartCoroutine(BossIntroRoutine());
    }

    /// <summary>
    /// 清水 ボス登場の流れ
    /// プレイヤーを止める → 道の障害物とアイテムを全部消す → ボス登場の演出 → 先の地面にギミックを置く → 3,2,1,GO で再開
    /// </summary>
    private IEnumerator BossIntroRoutine()
    {
        Player player = _player.GetComponent<Player>();

        //止める（時間も止まる）
        _gameManager.SetState(GameManager.GameState.Event);
        if (player) player.PausePlayer();

        //ふつうの障害物とアイテムを消して、ここからはボスのギミックを出す
        _ground._isBoss = true;
        _ground.meshInfo._generateObstacle.DeleteAllObstacles();
        _ground.meshInfo._itemGenerate.DeleteAllItems();
        BossHazard.DestroyAll();

        Vector3 pos = new Vector3(BossSpawnX, BossSpawnY, _player.position.z + BossSpawnAhead);
        GameObject boss = Instantiate(Current.BossPrefab, pos, Current.BossPrefab.transform.rotation);
        _currentBoss = boss.GetComponent<MotherComputer>();
        _currentBoss.SetBattleManager(this);

        //清水 ボスごとに見た目だけ付け替えられる
        if (Current.ModelOverride)
        {
            ModelSlot.Apply(boss, Current.ModelOverride, Current.ModelOffset, Current.ModelEuler, Current.ModelScale);
        }

        //何が始まったかを見せる
        GameHUD.Get().Banner(IsLastBoss ? "FINAL BATTLE!!" : "WARNING!!\n<size=60%>BOSS APPROACHING</size>", GameHUD.Danger, 2.2f);
        GameHUD.Get().ShowBossBar(Current.DisplayName, _currentBoss.MaxHp, _currentBoss.MaxHp);
        ScreenEffects.Get().Shake(0.5f, 0.8f);
        if (WarningSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(WarningSE);

        //ボス登場のカットシーン。終わるまで待つ
        float introTime = PlayCutScene(boss.transform);
        yield return new WaitForSecondsRealtime(Mathf.Max(introTime, MinIntroTime));

        //先の地面にふつうの障害物とギミックを置く（すぐ前とボスの手前は空けておく）
        foreach (GroundAutomatically chunk in _ground.groundScripts)
        {
            if (chunk.gameObject.activeInHierarchy && chunk.transform.position.z > _player.position.z + SafeDistanceOnStart)
            {
                if (AllowObstacles(chunk)) chunk.GenObsItem();
                SpawnHazards(chunk);
            }
        }

        //3,2,1,GO で再開
        for (int i = 3; i >= 1; i--)
        {
            GameHUD.Get().Banner("", Color.white, 0.5f, i);
            yield return new WaitForSecondsRealtime(0.6f);
        }
        GameHUD.Get().Banner("GO!!", GameHUD.Gold, 0.8f);

        if (player) player.ReStartPlayer();
        _gameManager.SetState(GameManager.GameState.Playing);
    }

    /// <summary>
    /// ボスに当てた（MotherComputer から呼ばれる。倒したときは OnBossDefeated）
    /// </summary>
    public void OnBossHit(MotherComputer boss, int hp, int maxHp, int damage, float speed, bool isCritical)
    {
        SpawnEffect(HitEffect, boss.transform.position);
        if (HitSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(HitSE);

        GameHUD.Get().ShowBossBar(Current.DisplayName, hp, maxHp);
        //速いほど大ダメージ
        string title = isCritical ? "CRITICAL!!" : "HIT!";
        GameHUD.Get().Banner(title, isCritical ? GameHUD.Danger : GameHUD.Gold, 1.2f, "-", damage);
    }

    //カットシーンを流して、その長さ（秒）を返す
    private float PlayCutScene(Transform boss)
    {
        if (!CutSceneDirectorObject) return 0f;

        CutSceneDirectorObject.SetActive(true);

        CineCameraController cine = CutSceneDirectorObject.GetComponent<CineCameraController>();
        if (cine) cine.SetTarget(boss);

        //2体目からは止まっているので最初から流し直す
        PlayableDirector director = CutSceneDirectorObject.GetComponent<PlayableDirector>();
        if (director)
        {
            director.time = 0;
            director.Play();
            return (float)director.duration;
        }
        return 0f;
    }

    /// <summary>
    /// ボスを倒した（MotherComputer から呼ばれる）
    /// </summary>
    public void OnBossDefeated(MotherComputer boss)
    {
        //倒したときは爆発を何発か重ねる
        GameObject defeatEffect = DefeatEffect ? DefeatEffect : HitEffect;
        for (int i = 0; i < 4; i++)
        {
            SpawnEffect(defeatEffect, boss.transform.position + Random.insideUnitSphere * 8f);
        }
        if (DefeatSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(DefeatSE);

        GameHUD.Get().ShowBossBar(Current.DisplayName, 0, boss.MaxHp);
        GameHUD.Get().Flash(new Color(1, 1, 1, 0.9f), 0.6f);
        ScreenEffects.Get().Shake(2.0f, 1.0f);
        ScreenEffects.Get().SlowMotion(0.25f, 1.0f);

        if (IsLastBoss)
        {
            //マザーコンピューターを倒した。世界を救った
            GameHUD.Get().Banner("MOTHER COMPUTER\nDESTROYED!!", GameHUD.Gold, ClearDelay + 1f);

            //クリア画面に行くまでの間にやられたり時間切れにならないように
            Player player = _player.GetComponent<Player>();
            if (player) player.SetInvincibleTime(ClearDelay + 1f);
            if (_timeManager) _timeManager.AddTime(ClearDelay + 5f);
            StartCoroutine(ClearRoutine());
            return;
        }

        bool useTime = _timeManager && _timeManager.UseTimeLimit;
        if (useTime) GameHUD.Get().Banner("BOSS DEFEATED!", GameHUD.Gold, 2.0f, "TIME +", Mathf.RoundToInt(TimeBonusOnDefeat));
        else GameHUD.Get().Banner("BOSS DEFEATED!", GameHUD.Gold, 2.0f);
        GameHUD.Get().HideBossBar();

        //爆発が見えるように少しだけ残してから消す
        Destroy(boss.gameObject, 0.3f);
        _currentBoss = null;

        //ボスのギミックを片付けて、ふつうのランに戻す
        if (_ramp) Destroy(_ramp.gameObject);
        _rampForZ = float.NaN;
        InBattle = false;
        _ground._isBoss = false;
        _ground.ConnectEnd();
        BossHazard.DestroyAll();

        if (_timeManager) _timeManager.AddTime(TimeBonusOnDefeat);

        _index++;
        _nextBossZ = _player.position.z + Current.RunDistance;
    }

    private void SpawnEffect(GameObject effect, Vector3 position)
    {
        if (!effect) return;
        Destroy(Instantiate(effect, position, Quaternion.identity), 3f);
    }

    private IEnumerator ClearRoutine()
    {
        yield return new WaitForSecondsRealtime(ClearDelay);
        _gameManager.SetState(GameManager.GameState.GameClear);
    }

    /// <summary>
    /// ボス戦中でもふつうの障害物を置いてよいか（ボスの手前は空ける）
    /// </summary>
    public bool AllowObstacles(GroundAutomatically chunk)
    {
        //清水 ボス戦中は地面の一部にだけ障害物を置く（ギミックと重なって難しすぎたので）
        return !IsNearBoss(chunk) && Random.value < ObstacleChanceInBattle;
    }

    //この地面がボスの手前（ジャンプ台を置くあたり）にかかっているか
    private bool IsNearBoss(GroundAutomatically chunk)
    {
        if (!_currentBoss) return false;

        float restZ = _currentBoss.RestZ;
        float chunkStart = chunk.chunkZ * chunk.ChunkLength;
        float chunkEnd = chunkStart + chunk.ChunkLength;
        return chunkEnd > restZ - NoHazardBeforeBoss && chunkStart < restZ + 100f;
    }

    /// <summary>
    /// ボス戦中に新しい地面ができたらギミックを置く（GroundAutomatically から呼ばれる）
    /// </summary>
    public void SpawnHazards(GroundAutomatically chunk)
    {
        if (!InBattle) return;

        //ボスの手前（ジャンプ台のあたり）は障害物を置かない
        if (IsNearBoss(chunk)) return;

        //清水 ギミックを置かない地面もはさんで、息をつけるようにする
        if (Random.value >= HazardChunkChance) return;

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

        //必ず通れる道の上には置かない
        Vector3 point = RandomPoint(chunk, 6f);
        if (chunk.Layout != null)
        {
            float lane = chunk.Layout.CorridorX(point.z);
            float keep = chunk.Layout.CorridorWidth * 0.5f + 4f;
            if (Mathf.Abs(point.x - lane) < keep)
            {
                point.x = lane + (point.x < lane ? -keep : keep);
                if (point.x < 6f || point.x > chunk.ChunkWidth - 6f) return; //道の外に置く場所がなければ置かない
                point.y = chunk.GetLocalHeight(point.x, point.z);
            }
        }
        GameObject root = CreateHazardRoot(chunk, "ElectricTentacle", point);

        GameObject basePrefab = ElectricBasePrefabs[Random.Range(0, ElectricBasePrefabs.Length)];
        GameObject baseObj = Instantiate(basePrefab, root.transform);
        baseObj.transform.localPosition = Vector3.zero;
        ModelSlot.EnsureCollider(baseObj, true); //新しいモデルでも当たるように

        if (TentaclePrefab)
        {
            GameObject tentacle = Instantiate(TentaclePrefab, root.transform);
            tentacle.transform.localPosition = Vector3.zero;
            tentacle.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            tentacle.transform.localScale *= TentacleScale;
        }

        root.AddComponent<ElectricHazard>();
        GenerateObstacle.AddOutline(root, new Color(0.3f, 0.9f, 1f), 6f); //一撃死は太く目立たせる
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
        burner.Setup(BurnerPrefab, chunk, BurnerSpacing, pattern, Random.Range(0f, 6f));
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
        GameObject trainObj = Instantiate(TrainPrefab, root.transform);
        trainObj.transform.localPosition = Vector3.zero;
        ModelSlot.EnsureCollider(trainObj, true); //新しいモデルでも当たるように
        GenerateObstacle.AddOutline(root, GameHUD.Danger, 6f);

        TrainHazard train = root.AddComponent<TrainHazard>();
        if (crossing)
            train.Setup(TrainHazard.Route.Crossing, dir, CrossingTrainSpeed, CrossingTriggerDistance, chunk.ChunkWidth + 60f, TrainWarningSE);
        else
            train.Setup(TrainHazard.Route.Oncoming, dir, OncomingTrainSpeed, OncomingTriggerDistance, 2000f, TrainWarningSE);
    }
}
