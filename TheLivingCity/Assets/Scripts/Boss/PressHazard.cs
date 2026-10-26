using UnityEngine;

//清水
//プレス機（触手がコンテナを持って、振り下ろし続ける）
//上で止まる → ぶるぶる震える（予告） → 一気に振り下ろす → 下りたまま → ゆっくり上がる、をくり返す
//コンテナの底が低いときに挟まれたら一撃死。上がっている間に下をくぐるか、よけて通る
public class PressHazard : MonoBehaviour
{
    [SerializeField] private Material ContainerMaterial;     //FBX にマテリアルが入っていないのでここで付ける
    [SerializeField] private float ContainerHeight = 4.7f;   //モデルのいちばん上からコンテナの底までの高さ（モデルの大きさのままのm）

    [Header("動き（秒）")]
    [SerializeField] private float UpTime = 1.2f;     //上で止まっている
    [SerializeField] private float WarnTime = 0.6f;   //振り下ろす前に震える
    [SerializeField] private float SlamTime = 0.2f;   //振り下ろす
    [SerializeField] private float DownTime = 1.0f;   //下りたまま
    [SerializeField] private float RiseTime = 1.0f;   //上がる

    [SerializeField] private float DeadlyHeight = 4.5f;  //コンテナの底がこれより低いと一撃死（プレス機の大きさに合わせてのびる）
    [SerializeField] private float CrushPadding = 0.8f;  //当たり判定をコンテナより少し広げる（前後左右、m）
    [SerializeField] private float WarnShake = 0.25f;    //予告で震える大きさ（m）
    [SerializeField] private float ShakeDistance = 45f;  //プレイヤーがこれより近いと、振り下ろしたときに画面がゆれる

    private Transform _model;
    private Vector3 _modelBase;
    private BoxCollider _crush;
    private DangerMarker _marker;
    private Transform _player;

    private float _bottom;   //上にあるときのコンテナの底の高さ
    private float _time;
    private bool _slammed;

    private float Cycle => UpTime + WarnTime + SlamTime + DownTime + RiseTime;

    /// <summary>
    /// 動きのどこから始めるか（0〜1）。並べたときにばらばらに動くように
    /// </summary>
    public void SetPhase(float phase01)
    {
        _time = phase01 * Cycle;
    }

    private void Start()
    {
        _model = transform.childCount > 0 ? transform.GetChild(0) : transform;
        _modelBase = _model.localPosition;

        Renderer[] renderers = _model.GetComponentsInChildren<Renderer>();
        if (ContainerMaterial)
        {
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = ContainerMaterial;
                r.sharedMaterials = mats;
            }
        }

        //モデルの大きさを測って、コンテナの部分（いちばん上から ContainerHeight 分）に当たり判定を付ける
        Vector3 min = new Vector3(-5f, 7f, -2.3f);
        Vector3 max = new Vector3(5f, 11.7f, 2.3f);
        bool found = false;
        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 local = _model.InverseTransformPoint(corner);
                if (!found) { min = max = local; found = true; }
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        min.y = Mathf.Max(min.y, max.y - ContainerHeight);
        _bottom = Mathf.Max(0f, _modelBase.y + min.y * _model.localScale.y);

        GameObject crushObj = new GameObject("CrushZone");
        crushObj.transform.SetParent(_model, false);
        _crush = crushObj.AddComponent<BoxCollider>();
        _crush.center = (min + max) * 0.5f;
        _crush.size = max - min + new Vector3(CrushPadding * 2f, 0f, CrushPadding * 2f);

        BossHazard.MakeDeadly(gameObject);
        _crush.enabled = false;

        //コンテナが落ちてくる場所に、しるしを出す
        Vector3 center = transform.InverseTransformPoint(_model.TransformPoint(_crush.center));
        Vector3 size = Vector3.Scale(_crush.size, _model.lossyScale);
        _marker = DangerMarker.Create(transform, new Vector3(center.x, 0.3f, center.z), new Vector2(size.x, size.z), GameHUD.Danger);

        Player player = FindAnyObjectByType<Player>();
        if (player) _player = player.transform;
    }

    private void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Repeat(_time, Cycle);

        float drop = 0f;      //0 で上、1 で下
        bool warn = false;
        bool falling = false;

        if (t < UpTime)
        {
            _slammed = false;
        }
        else if ((t -= UpTime) < WarnTime)
        {
            warn = true;
        }
        else if ((t -= WarnTime) < SlamTime)
        {
            float k = t / SlamTime;
            drop = k * k; //だんだん速く
            falling = true;
        }
        else if ((t -= SlamTime) < DownTime)
        {
            drop = 1f;
            if (!_slammed) OnSlam();
        }
        else
        {
            t -= DownTime;
            drop = 1f - Mathf.SmoothStep(0f, 1f, t / RiseTime);
        }

        Vector3 shake = Vector3.zero;
        if (warn)
        {
            Vector2 r = Random.insideUnitCircle * WarnShake;
            shake = new Vector3(r.x, 0f, r.y);
        }
        _model.localPosition = _modelBase + shake + Vector3.down * (_bottom * drop);

        //コンテナの底が低いときだけ当たる（上がっている間は下をくぐれる）
        float bottomNow = _bottom * (1f - drop);
        bool deadly = bottomNow < DeadlyHeight;
        if (_crush.enabled != deadly) _crush.enabled = deadly;

        _marker.SetAlert(warn || falling || drop >= 1f);
    }

    private void OnSlam()
    {
        _slammed = true;
        if (_player && Vector3.Distance(_player.position, transform.position) < ShakeDistance)
            ScreenEffects.Get().Shake(0.35f, 0.25f);
    }
}
