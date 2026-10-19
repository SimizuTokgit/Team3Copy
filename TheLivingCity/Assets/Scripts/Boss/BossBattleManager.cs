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

    //HUD 用
    public int BossNumber => _index + 1;
    public int BossCount => Encounters.Count;
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

        //何が始まったかを見せる
        GameHUD.Get().Banner(IsLastBoss ? "FINAL BATTLE!!" : "WARNING!!\n<size=60%>BOSS APPROACHING</size>", GameHUD.Danger, 2.2f);
        GameHUD.Get().ShowBossBar(Current.DisplayName, _currentBoss.MaxHp, _currentBoss.MaxHp);
        ScreenEffects.Get().Shake(0.5f, 0.8f);
        if (WarningSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(WarningSE);

        //ボス登場のカットシーン。終わるまで待つ
        float introTime = PlayCutScene(boss.transform);
        yield return new WaitForSecondsRealtime(Mathf.Max(introTime, MinIntroTime));

        //先の地面にギミックを置く（すぐ前は空けておく）
        foreach (GroundAutomatically chunk in _ground.groundScripts)
        {
            if (chunk.gameObject.activeInHierarchy && chunk.transform.position.z > _player.position.z + SafeDistanceOnStart)
            {
                SpawnHazards(chunk);
            }
        }

        //3,2,1,GO で再開
        for (int i = 3; i >= 1; i--)
        {
            GameHUD.Get().Banner(i.ToString(), Color.white, 0.5f);
            yield return new WaitForSecondsRealtime(0.6f);
        }
        GameHUD.Get().Banner("GO!!", GameHUD.Gold, 0.8f);

        if (player) player.ReStartPlayer();
        _gameManager.SetState(GameManager.GameState.Playing);
    }

    /// <summary>
    /// ボスに当てた（MotherComputer から呼ばれる。倒したときは OnBossDefeated）
    /// </summary>
    public void OnBossHit(MotherComputer boss, int hp, int maxHp)
    {
        SpawnEffect(HitEffect, boss.transform.position);
        if (HitSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(HitSE);

        GameHUD.Get().ShowBossBar(Current.DisplayName, hp, maxHp);
        GameHUD.Get().Banner($"HIT!  {maxHp - hp}/{maxHp}", GameHUD.Gold, 1.0f);
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
        GameHUD.Get().Banner(useTime ? $"BOSS DEFEATED!\n<size=55%>TIME +{TimeBonusOnDefeat:0}</size>" : "BOSS DEFEATED!", GameHUD.Gold, 2.0f);
        GameHUD.Get().HideBossBar();

        //爆発が見えるように少しだけ残してから消す
        Destroy(boss.gameObject, 0.3f);
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
        GenerateObstacle.AddOutline(root, GameHUD.Danger, 6f);

        TrainHazard train = root.AddComponent<TrainHazard>();
        if (crossing)
            train.Setup(TrainHazard.Route.Crossing, dir, CrossingTrainSpeed, CrossingTriggerDistance, chunk.ChunkWidth + 60f, TrainWarningSE);
        else
            train.Setup(TrainHazard.Route.Oncoming, dir, OncomingTrainSpeed, OncomingTriggerDistance, 2000f, TrainWarningSE);
    }
}
