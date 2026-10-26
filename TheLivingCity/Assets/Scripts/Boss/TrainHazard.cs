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

    //清水 攻撃範囲（電車が通る帯）
    private DangerLane _lane;
    private float _halfLength;                  //進む向きの長さの半分
    private const float LaneMargin = 0.6f;      //帯は電車の幅より少しだけ広く
    private const float CrossingPreview = 1.8f; //横切る電車は、走り出す距離のこの倍から帯をうすく出しておく

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

        _renderers = GetComponentsInChildren<Renderer>();
        _halfLength = Extent(_moveDir);

        //清水 飛んでくる電車は、飛び出すまで見えないようにしておく
        if (_route == Route.Flying)
        {
            _modelRotation = Quaternion.Inverse(Quaternion.LookRotation(_moveDir)) * transform.rotation;
            SetVisible(false);
        }
    }

    //消えたときと、地面ごとしまわれたとき（地面の使い回し）に帯も消す
    private void OnDisable()
    {
        if (_lane) Destroy(_lane.gameObject);
        _lane = null;
    }

    //清水 電車の通り道を地面に出す（幅は電車の幅、高さは電車の半分くらいまで壁を立てる）
    private void ShowLane(Vector3 from, Vector3 to, bool alert)
    {
        if (!_lane)
        {
            Vector3 side = Vector3.Cross(Vector3.up, _moveDir).normalized;
            float width = Extent(side) * 2f + LaneMargin;
            float wall = Mathf.Clamp(Extent(Vector3.up) * 0.6f, 2.5f, 8f);
            _lane = DangerLane.Create(from, to, width, GameHUD.Danger, wall);
        }
        _lane.SetAlert(alert);
    }

    //電車の先頭（ここより後ろの帯は消す）
    private Vector3 FrontPoint => transform.position + _moveDir * _halfLength;

    private void Update()
    {
        if (!_player) return;

        Vector3 playerPos = _player.transform.position;

        if (!_isRunning)
        {
            //清水 横切る電車は、近づいたら通り道をうすく出しておく
            if (_route == Route.Crossing && !_lane && transform.position.z - playerPos.z <= _triggerDistance * CrossingPreview)
            {
                ShowLane(transform.position, transform.position + _moveDir * _travelDistance, false);
            }

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
                    ShowLane(FrontPoint, new Vector3(playerPos.x, playerPos.y, playerPos.z - 20f), true);
                }
                else
                {
                    //どっちから来るかを画面の端に出す
                    GameHUD.Get().WarnTrain(_moveDir.x > 0f);
                    ShowLane(transform.position, transform.position + _moveDir * _travelDistance, true);
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
        if (_lane) _lane.SetStartPoint(FrontPoint);

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

        //電車が通る列を地面に赤く出す（落ちてくるところから、プレイヤーの後ろまで）
        _halfLength = Extent(_moveDir);
        ShowLane(new Vector3(x, playerPos.y, pos.z), new Vector3(x, playerPos.y, playerPos.z - 20f), true);

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
        if (_lane) _lane.SetStartPoint(FrontPoint);

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

    //見た目の大きさ：位置から axis の向きにどこまで出ているか
    private float Extent(Vector3 axis)
    {
        float best = 0f;
        Vector3 c = transform.position;
        if (_renderers == null) return 1f;
        foreach (Renderer r in _renderers)
        {
            if (!r || r is ParticleSystemRenderer) continue;
            Bounds b = r.bounds;
            if (b.size == Vector3.zero) continue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                best = Mathf.Max(best, Vector3.Dot(p - c, axis));
            }
        }
        return best > 0f ? best : 1f;
    }
}
