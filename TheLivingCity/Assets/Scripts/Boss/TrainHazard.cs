using UnityEngine;

//清水
//走ってくる電車（中ボス4の攻撃）
//横切る電車と正面から来る電車の2種類。触れたら一撃死
public class TrainHazard : MonoBehaviour
{
    public enum Route
    {
        Crossing, //道を横切る
        Oncoming  //正面から向かってくる
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

                //正面から来る電車はプレイヤーのいる列に合わせる
                if (_route == Route.Oncoming)
                {
                    transform.position = new Vector3(playerPos.x, transform.position.y, transform.position.z);
                }
            }
            return;
        }

        //走っている間は赤く点滅
        _warningLight.intensity = Mathf.PingPong(Time.time * 20f, 6f);

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

        if (_route == Route.Oncoming) return distanceZ <= _triggerDistance;

        //横切る電車は、プレイヤーがここに着くころに先頭がプレイヤーの列に来るように走り出す
        //電車の来ない側へハンドルを切れば避けられる
        if (distanceZ > _triggerDistance) return false;

        float playerSpeed = Mathf.Max(10f, _player.Physics.GetVelocity().z);
        float playerArriveTime = distanceZ / playerSpeed;
        float trainArriveTime = Mathf.Abs(playerPos.x - transform.position.x) / _speed;

        return playerArriveTime <= trainArriveTime;
    }
}
