using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PlayerCamera : MonoBehaviour
{
    private CinemachineCamera cinemachine;
    private PostProcessVolume postProcessVolume;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;

    private readonly float DefaultFov = 60.0f;
    private float targetFov = 60.0f;
    private readonly float targetSpeed = 70.0f;

    [Range(0,1)] private float targetAberrationIntensity = 0;
    [Range(-60,0)]private float targetDistortionIntensity = 0;

    private void Awake()
    {
        cinemachine = GetComponent<CinemachineCamera>();
        cinemachine.Lens.FieldOfView = DefaultFov;

        postProcessVolume = FindAnyObjectByType<PostProcessVolume>();
        postProcessVolume.profile.TryGetSettings<ChromaticAberration>(out chromaticAberration);
        postProcessVolume.profile.TryGetSettings<LensDistortion>(out lensDistortion);

    }

    private void Update()
    {
        if (cinemachine.Lens.FieldOfView != targetFov)
            cinemachine.Lens.FieldOfView = Mathf.MoveTowards(cinemachine.Lens.FieldOfView, targetFov, targetSpeed * Time.deltaTime);

        if (chromaticAberration.intensity.value != targetAberrationIntensity)
            chromaticAberration.intensity.value = Mathf.MoveTowards(chromaticAberration.intensity.value, targetAberrationIntensity, 4 * Time.deltaTime);

        if (lensDistortion.intensity.value != targetDistortionIntensity)
            lensDistortion.intensity.value = Mathf.MoveTowards(lensDistortion.intensity.value, targetDistortionIntensity, 240 * Time.deltaTime);
    }

    public void SetTargetFov(float target)
    {
        targetFov = target;
    }

    public void FovReSet()
    {
        targetFov = DefaultFov;
    }

    public void JustLandEffect()
    {
        targetAberrationIntensity = 1;
        targetDistortionIntensity = -60;
    }

    public void JustLandEffectOff()
    {
        targetAberrationIntensity = 0;
        targetDistortionIntensity = 0;
    }
}