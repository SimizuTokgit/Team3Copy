using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

//安井
//ダメージを受けた時の演出
public class DamageScreen : MonoBehaviour
{
    [SerializeField] private PostProcessVolume volume;

    private Vignette vignette;

    [SerializeField] private float DAMAGE_INTENDITY = 0.625f; //赤枠の太さ
    [SerializeField] private float RETURN_SPEED = 2f;         //戻す速度

    private float _returnSpeed;

    private void Start()
    {
        volume.profile.TryGetSettings(out vignette);
        _returnSpeed = RETURN_SPEED;
    }

    private void Update()
    {
        Remaining();
    }

    /// <summary>
    /// ダメージを受ける演出
    /// </summary>
    public void Damage()
    {
        vignette.intensity.value = DAMAGE_INTENDITY;
    }

    /// <summary>
    /// 徐々に元に戻す
    /// </summary>
    private void Remaining()
    {
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, Time.deltaTime * _returnSpeed);
    }
}