using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

//安井
//モーションブラー処理
public class MotionBlurController : MonoBehaviour
{
    [SerializeField] private PostProcessVolume volume;

    private MotionBlur MotionBlur;
    private Player Player;
    private Rigidbody Rigidboby;

    [SerializeField] private float thresholdSpeed = 100f; // km/h

    void Start()
    {
        volume.profile.TryGetSettings(out MotionBlur);

        Player = FindAnyObjectByType<Player>();
        Rigidboby = Player.GetComponent<Rigidbody>();
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
        // m/s → km/h 変換
        float speedKmh = Rigidboby.linearVelocity.magnitude * 3.6f;

        // 閾値チェック
        MotionBur(speedKmh >= thresholdSpeed);
    }

    /// <summary>
    /// モーションブラーの処理
    /// </summary>
    private void MotionBur(bool isActive)
    {
        if (isActive)
        {
            MotionBlur.active = true;

            MotionBlur.shutterAngle.overrideState = true;
            MotionBlur.sampleCount.overrideState = true;

            MotionBlur.shutterAngle.value = 360f;
            MotionBlur.sampleCount.value = 32;
        }
        else
        {
            MotionBlur.active = false;
        }
    }
}