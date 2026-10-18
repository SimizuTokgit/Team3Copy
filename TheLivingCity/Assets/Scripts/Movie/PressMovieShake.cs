using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

// Timelineの再生中、決めた秒でカメラを揺らす
// 絵コンテのC6（崩落）とC7（着弾）で使う
public class PressMovieShake : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private CinemachineImpulseSource impulse;

    [Header("揺らす秒（Timelineの時間）")]
    [SerializeField] private float[] shakeTimes = { 2.75f, 2.95f };

    [Header("それぞれの強さ")]
    [SerializeField] private float[] shakeForces = { 0.4f, 1.2f };

    private int nextIndex = 0;
    private double lastTime = -1;

    private void Reset()
    {
        director = GetComponent<PlayableDirector>();
        impulse = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
        if (!director || !impulse) return;

        double time = director.time;

        // 巻き戻されたら最初から数え直す
        if (time < lastTime) nextIndex = 0;
        lastTime = time;

        while (nextIndex < shakeTimes.Length && time >= shakeTimes[nextIndex])
        {
            float force = (nextIndex < shakeForces.Length) ? shakeForces[nextIndex] : 1f;
            impulse.GenerateImpulseWithForce(force);
            nextIndex++;
        }
    }
}
