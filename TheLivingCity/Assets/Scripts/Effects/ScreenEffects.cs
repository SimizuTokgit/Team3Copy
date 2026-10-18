using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//清水
//画面効果まとめ（URP用）
//今までの赤枠やモーションブラーは Post Processing Stack v2 で作られていて、URP では画面に出ていなかった
//ここで URP の Volume を自分で作って、揺れ・ヒットストップ・赤枠・色ズレ・フォグなどを出す
//シーンに置かなくても ScreenEffects.Get() で勝手にできる
[DefaultExecutionOrder(10000)] //Cinemachine がカメラを動かした後に揺らす
public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }

    public static ScreenEffects Get()
    {
        if (!Instance) new GameObject("ScreenEffects").AddComponent<ScreenEffects>();
        return Instance;
    }

    [Header("色味（崩壊した街っぽく少し暗く、こく）")]
    [SerializeField] private float Contrast = 18f;
    [SerializeField] private float Saturation = -12f;
    [SerializeField] private Color ColorFilter = new Color(1f, 0.93f, 0.85f);
    [SerializeField] private float BloomIntensity = 0.6f;

    [Header("フォグ（遠くの地面が急に出てくるのを隠す）")]
    [SerializeField] private bool UseFog = true;
    [SerializeField] private Color FogColor = new Color(0.55f, 0.47f, 0.42f);
    [SerializeField] private float FogStart = 120f;
    [SerializeField] private float FogEnd = 650f;

    [Header("ダメージ")]
    [SerializeField] private Color DamageColor = new Color(0.9f, 0f, 0f);
    [SerializeField] private float DamageVignette = 0.5f;

    private Volume _volume;
    private Vignette _vignette;
    private ChromaticAberration _chromatic;
    private LensDistortion _lens;
    private MotionBlur _motionBlur;
    private ColorAdjustments _color;

    private float _vignettePulse;
    private float _chromaticPulse;
    private float _exposurePulse;
    private bool _isJustLand;
    private float _speedBlur;

    private float _shakeAmplitude;
    private float _shakeTime;
    private float _shakeDuration;

    private Coroutine _timeCoroutine;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateVolume();
        EnableCameraPostProcess();

        if (UseFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;
        }
    }

    private void Start()
    {
        //Awake の時点でカメラがまだ無いこともあるので、もう一度
        EnableCameraPostProcess();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        //ヒットストップ中にシーンが変わっても止まったままにならないように
        Time.timeScale = 1f;
    }

    private void CreateVolume()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        _vignette = profile.Add<Vignette>(true);
        _vignette.intensity.value = 0f;
        _vignette.smoothness.value = 0.6f;
        _vignette.color.value = DamageColor;

        _chromatic = profile.Add<ChromaticAberration>(true);
        _chromatic.intensity.value = 0f;

        _lens = profile.Add<LensDistortion>(true);
        _lens.intensity.value = 0f;

        _motionBlur = profile.Add<MotionBlur>(true);
        _motionBlur.intensity.value = 0f;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = BloomIntensity;
        bloom.threshold.value = 1.1f;

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.value = TonemappingMode.ACES;

        _color = profile.Add<ColorAdjustments>(true);
        _color.contrast.value = Contrast;
        _color.saturation.value = Saturation;
        _color.colorFilter.value = ColorFilter;
        _color.postExposure.value = 0f;

        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 100f;
        _volume.sharedProfile = profile;
    }

    private void EnableCameraPostProcess()
    {
        foreach (Camera cam in Camera.allCameras)
        {
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data) data.renderPostProcessing = true;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        _vignettePulse = Mathf.MoveTowards(_vignettePulse, 0f, dt * 1.2f);
        _chromaticPulse = Mathf.MoveTowards(_chromaticPulse, 0f, dt * 2.5f);
        _exposurePulse = Mathf.MoveTowards(_exposurePulse, 0f, dt * 6f);

        _vignette.intensity.value = Mathf.Max(_vignettePulse, 0.18f); //少しだけ常に暗くして画面を締める
        _vignette.color.value = _vignettePulse > 0.01f ? DamageColor : Color.black;

        float chromaticTarget = _isJustLand ? 1f : _speedBlur * 0.35f;
        _chromatic.intensity.value = Mathf.Max(_chromaticPulse, Mathf.MoveTowards(_chromatic.intensity.value, chromaticTarget, dt * 4f));

        float lensTarget = _isJustLand ? -0.5f : 0f;
        _lens.intensity.value = Mathf.MoveTowards(_lens.intensity.value, lensTarget, dt * 4f);

        _motionBlur.intensity.value = Mathf.MoveTowards(_motionBlur.intensity.value, _speedBlur * 0.6f, dt * 2f);

        _color.postExposure.value = _exposurePulse;
    }

    private void LateUpdate()
    {
        if (_shakeTime <= 0f) return;

        _shakeTime -= Time.unscaledDeltaTime;
        Camera cam = Camera.main;
        if (!cam) return;

        float t = Mathf.Clamp01(_shakeTime / _shakeDuration);
        float a = _shakeAmplitude * t * t;
        cam.transform.position += cam.transform.right * Random.Range(-a, a) + cam.transform.up * Random.Range(-a, a);
    }

    /// <summary>
    /// ダメージを受けた（赤枠＋色ズレ＋揺れ）
    /// </summary>
    public void Damage(float strength = 1f)
    {
        _vignettePulse = Mathf.Max(_vignettePulse, DamageVignette * strength);
        _chromaticPulse = Mathf.Max(_chromaticPulse, 0.8f * strength);
        Shake(0.6f * strength, 0.3f);
    }

    /// <summary>
    /// カメラを揺らす
    /// </summary>
    public void Shake(float amplitude, float duration)
    {
        if (amplitude < _shakeAmplitude * Mathf.Clamp01(_shakeTime / Mathf.Max(_shakeDuration, 0.01f))) return;

        _shakeAmplitude = amplitude;
        _shakeDuration = Mathf.Max(duration, 0.01f);
        _shakeTime = _shakeDuration;
    }

    /// <summary>
    /// 一瞬明るくする（白フラッシュ）
    /// </summary>
    public void FlashExposure(float amount)
    {
        _exposurePulse = Mathf.Max(_exposurePulse, amount);
    }

    /// <summary>
    /// ジャスト着地のゆがみ（PlayerCamera から）
    /// </summary>
    public void SetJustLand(bool isOn)
    {
        _isJustLand = isOn;
    }

    /// <summary>
    /// 速さに合わせたブラー（0〜1）
    /// </summary>
    public void SetSpeedBlur(float amount01)
    {
        _speedBlur = Mathf.Clamp01(amount01);
    }

    /// <summary>
    /// ヒットストップ（一瞬止める）
    /// </summary>
    public void HitStop(float duration, float timeScale = 0.05f)
    {
        ChangeTime(timeScale, duration);
    }

    /// <summary>
    /// スローモーション
    /// </summary>
    public void SlowMotion(float timeScale, float duration)
    {
        ChangeTime(timeScale, duration);
    }

    private void ChangeTime(float timeScale, float duration)
    {
        if (_timeCoroutine != null) StopCoroutine(_timeCoroutine);
        _timeCoroutine = StartCoroutine(ChangeTimeRoutine(timeScale, duration));
    }

    private IEnumerator ChangeTimeRoutine(float timeScale, float duration)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        _timeCoroutine = null;
    }
}
