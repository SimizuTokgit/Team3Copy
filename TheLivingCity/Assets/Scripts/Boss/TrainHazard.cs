using UnityEngine;

//清水
//走ってくる電車（中ボス4の攻撃）
//横切る電車と正面から来る電車、空から飛んでくる大きい電車の3種類。触れたら一撃死
public class TrainHazard : MonoBehaviour
{
    public enum Route
    {
        Crossing, //道を横切る
        Oncoming, //正面から向かってくる
        Flying    //清水 大きい電車が空から飛んできて、プレイヤーの列に落ちてくる。左右どちらかによけないと当たる
    }

    private Route _route;
    private Vector3 _moveDir;     //ワールド座標での進む向き
    private float _speed;
    private float _triggerDistance;
    private float _travelDistance;
    private AudioClip _warningSE;

    private Player _player;
    private Light _warningLight;
    private bool _isRunning = false;
    private float _moved = 0f;

    //Flying 用
    private float _warningTime;
    private float _flyHeight;
    private float _flyTime;
    private float _roadMinX;
    private float _roadMaxX;
    private float _flyElapsed;
    private bool _landed;
    private Quaternion _modelRotation = Quaternion.identity; //向きに対するモデルのずれ
    private Renderer[] _renderers;
    private DangerMarker _laneMarker;

    /// <summary>
    /// 電車の動きを設定する（BossBattleManager から呼ぶ）
    /// </summary>
    public void Setup(Route route, Vector3 moveDir, float speed, float triggerDistance, float travelDistance, AudioClip warningSE)
    {
        _route = route;
        _moveDir = moveDir.normalized;
        _speed = speed;
        _triggerDistance = triggerDistance;
        _travelDistance = travelDistance;
        _warningSE = warningSE;
    }

    /// <summary>
    /// 清水 空から飛んでくる大きい電車にする（BossBattleManager から呼ぶ）
    /// warningTime 秒前に飛び出して、flyTime 秒かけて flyHeight の高さから道に落ちる。そのあとは道をすべってくる
    /// roadMinX〜roadMaxX はワールド座標での道の左右の端
    /// </summary>
    public void SetupFlying(float speed, float warningTime, float flyHeight, float flyTime, float roadMinX, float roadMaxX, AudioClip warningSE)
    {
        Setup(Route.Flying, Vector3.back, speed, 0f, 3000f, warningSE);
        _warningTime = warningTime;
        _flyHeight = flyHeight;
        _flyTime = Mathf.Max(0.1f, flyTime);
        _roadMinX = roadMinX;
        _roadMaxX = roadMaxX;
    }

    private void Start()
    {
        BossHazard.MakeDeadly(gameObject);

        _player = FindAnyObjectByType<Player>();

        //来るぞという合図の赤いライト
        GameObject lightObj = new GameObject("TrainWarningLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0, 8f, 0);
        _warningLight = lightObj.AddComponent<Light>();
        _warningLight.type = LightType.Point;
        _warningLight.color = Color.red;
        _warningLight.range = 40f;
        _warningLight.intensity = 0f;

        //清水 飛んでくる電車は、飛び出すまで見えないようにしておく
        if (_route == Route.Flying)
        {
            _modelRotation = Quaternion.Inverse(Quaternion.LookRotation(_moveDir)) * transform.rotation;
            _renderers = GetComponentsInChildren<Renderer>();
            SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        if (_laneMarker) Destroy(_laneMarker.gameObject);
    }

    private void Update()
    {
        if (!_player) return;

        Vector3 playerPos = _player.transform.position;

        if (!_isRunning)
        {
            if (ShouldStart(playerPos))
            {
                _isRunning = true;
                if (_warningSE && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(_warningSE);

                if (_route == Route.Flying)
                {
                    LaunchFlying(playerPos);
                    return;
                }

                //正面から来る電車はプレイヤーのいる列に合わせる
                if (_route == Route.Oncoming)
                {
                    transform.position = new Vector3(playerPos.x, transform.position.y, transform.position.z);
                    GameHUD.Get().Popup("TRAIN AHEAD!", GameHUD.Danger);
                }
                else
                {
                    //どっちから来るかを画面の端に出す
                    GameHUD.Get().WarnTrain(_moveDir.x > 0f);
                }
            }
            return;
        }

        //走っている間は赤く点滅
        _warningLight.intensity = Mathf.PingPong(Time.time * 20f, 6f);

        if (_route == Route.Flying)
        {
            UpdateFlying(playerPos);
            return;
        }

        float step = _speed * Time.deltaTime;
        transform.position += _moveDir * step;
        _moved += step;

        //走りきったら消す。正面から来る電車はプレイヤーの後ろに抜けたら消す
        if (_moved >= _travelDistance ||
            (_route == Route.Oncoming && transform.position.z < playerPos.z - 50f))
        {
            Destroy(gameObject);
        }
    }

    private bool ShouldStart(Vector3 playerPos)
    {
        float distanceZ = transform.position.z - playerPos.z;

        //清水 飛んでくる電車は、プレイヤーとぶつかる warningTime 秒前に飛び出す
        if (_route == Route.Flying)
        {
            float closing = Mathf.Max(10f, _player.Physics.GetVelocity().z) + _speed;
            return distanceZ <= closing * _warningTime;
        }

        if (_route == Route.Oncoming) return distanceZ <= _triggerDistance;

        //横切る電車は、プレイヤーがここに着くころに先頭がプレイヤーの列に来るように走り出す
        //電車の来ない側へハンドルを切れば避けられる
        if (distanceZ > _triggerDistance) return false;

        float playerSpeed = Mathf.Max(10f, _player.Physics.GetVelocity().z);
        float playerArriveTime = distanceZ / playerSpeed;
        float trainArriveTime = Mathf.Abs(playerPos.x - transform.position.x) / _speed;

        return playerArriveTime <= trainArriveTime;
    }

    //清水 プレイヤーのいる列の上空に出して、落ちてくる列にしるしを出す
    private void LaunchFlying(Vector3 playerPos)
    {
        SetVisible(true); //見えていないと大きさが測れない
        float half = HalfWidth();
        float minX = _roadMinX + half * 0.6f;
        float maxX = _roadMaxX - half * 0.6f;
        float x = minX < maxX ? Mathf.Clamp(playerPos.x, minX, maxX) : (_roadMinX + _roadMaxX) * 0.5f;

        Vector3 pos = transform.position;
        pos.x = x;
        pos.y = GroundHeight(x, pos.z, playerPos.y) + _flyHeight;
        transform.position = pos;

        //電車が通る列を地面に赤く出す（プレイヤーの少し前から、落ちてくるところまで）
        float startZ = playerPos.z + 5f;
        float length = Mathf.Max(10f, pos.z - startZ);
        float midZ = startZ + length * 0.5f;
        _laneMarker = DangerMarker.Create(null, new Vector3(x, GroundHeight(x, midZ, playerPos.y) + 0.3f, midZ), new Vector2(half * 2f, length), GameHUD.Danger);
        _laneMarker.SetAlert(true);

        GameHUD.Get().Popup("BIG TRAIN!! DODGE!", GameHUD.Danger);
    }

    private void UpdateFlying(Vector3 playerPos)
    {
        _flyElapsed += Time.deltaTime;

        Vector3 pos = transform.position + _moveDir * (_speed * Time.deltaTime);
        float ground = GroundHeight(pos.x, pos.z, pos.y);

        if (_flyElapsed < _flyTime)
        {
            //だんだん速く落ちる（放物線）。落ちる向きに頭を下げる
            float k = _flyElapsed / _flyTime;
            pos.y = ground + _flyHeight * (1f - k * k);
            float fallSpeed = 2f * _flyHeight * k / _flyTime;
            Vector3 velocity = _moveDir * _speed + Vector3.down * fallSpeed;
            transform.rotation = Quaternion.LookRotation(velocity) * _modelRotation;
        }
        else
        {
            pos.y = ground;
            transform.rotation = Quaternion.LookRotation(_moveDir) * _modelRotation;
            if (!_landed)
            {
                _landed = true;
                if (Mathf.Abs(pos.z - playerPos.z) < 150f) ScreenEffects.Get().Shake(0.5f, 0.35f);
            }
        }
        transform.position = pos;

        //プレイヤーの後ろに抜けたら消す
        if (pos.z < playerPos.z - 60f) Destroy(gameObject);
    }

    //地面の高さ（見つからなければ fallback）
    private static float GroundHeight(float x, float z, float fallback)
    {
        int mask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(new Vector3(x, fallback + 300f, z), Vector3.down, out RaycastHit hit, 600f, mask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return fallback;
    }

    //電車の横幅の半分（見た目の大きさから測る）
    private float HalfWidth()
    {
        float half = 0f;
        foreach (Renderer r in _renderers)
        {
            if (r && !(r is ParticleSystemRenderer)) half = Mathf.Max(half, Mathf.Max(r.bounds.max.x - transform.position.x, transform.position.x - r.bounds.min.x));
        }
        return half > 0f ? half : 5f;
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in _renderers)
        {
            if (r) r.enabled = visible;
        }
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
        {
            c.enabled = visible;
        }
    }
}
