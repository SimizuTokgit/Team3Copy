using UnityEngine;

//安井
//モーションブラー処理
//清水 URP では Post Processing Stack v2 が出ないので ScreenEffects で出す。速さに合わせて強くする
public class MotionBlurController : MonoBehaviour
{
    private Rigidbody Rigidboby;

    [SerializeField] private float thresholdSpeed = 160f; // km/h ここからブラーがかかり始める
    [SerializeField] private float maxSpeed = 300f;       // km/h ここで最大

    void Start()
    {
        Player player = FindAnyObjectByType<Player>();
        if (player) Rigidboby = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        PlayerSpeedCheck();
    }

    /// <summary>
    /// プレイヤーの速度判定
    /// </summary>
    private void PlayerSpeedCheck()
    {
        if (!Rigidboby) return;

        // m/s → km/h 変換
        float speedKmh = Rigidboby.linearVelocity.magnitude * 3.6f;

        ScreenEffects.Get().SetSpeedBlur(Mathf.InverseLerp(thresholdSpeed, maxSpeed, speedKmh));
    }
}
