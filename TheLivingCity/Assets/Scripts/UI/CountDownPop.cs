using UnityEngine;

// 小さい状態からドンと大きく出して、一度行き過ぎてから戻って止まる演出
// カウントダウンの数字と GO に付ける（CountDownManager が付いていなければ自動で付ける）
[RequireComponent(typeof(RectTransform))]
public class CountDownPop : MonoBehaviour
{
    [Header("大きさ")]
    [SerializeField] private float startScale = 0.2f; // 出た瞬間の大きさ（小さいところから出す）
    [SerializeField] private float endScale = 1.0f;   // 落ち着いたときの大きさ
    [SerializeField] private float duration = 0.25f;  // 大きくなりきるまでの時間

    [Header("行き過ぎ")]
    // 大きいほど一度大きく行き過ぎてから戻る。0で行き過ぎなし
    // 1.7で約1割、2.5で約2割、3.5で約3割 行き過ぎる
    [SerializeField] private float bounce = 2.5f;

    [Header("仕上げ")]
    [SerializeField] private bool fadeIn = true; // 出はじめだけ薄くする

    [Header("GO用 止まったあと小さく脈打たせる")]
    [SerializeField] private bool pulse = false;
    [SerializeField] private float pulseScale = 0.04f;
    [SerializeField] private float pulseSpeed = 8f;

    private RectTransform rect;
    private CanvasGroup group;

    private float time;
    private bool playing;

    private void Awake()
    {
        rect = (RectTransform)transform;

        if (fadeIn)
        {
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    // CountDownManager から自動で付けたときに強さを決める
    public void Setup(float startScale, float duration, float bounce, bool pulse)
    {
        this.startScale = startScale;
        this.duration = duration;
        this.bounce = bounce;
        this.pulse = pulse;
    }

    // SetActive で出すもの（GOなど）は、出た瞬間に勝手に再生する
    private void OnEnable()
    {
        Play();
    }

    // 数字のように出しっぱなしのものは、切り替わるたびにこれを呼ぶ
    public void Play()
    {
        if (!rect) rect = (RectTransform)transform;

        time = 0f;
        playing = true;
        Apply(0f);
    }

    private void Update()
    {
        if (playing)
        {
            time += Time.deltaTime;
            float t = (duration > 0f) ? Mathf.Clamp01(time / duration) : 1f;
            Apply(t);

            if (t >= 1f) playing = false;
            return;
        }

        if (pulse) rect.localScale = Vector3.one * (endScale + Mathf.Sin(Time.time * pulseSpeed) * pulseScale);
    }

    private void Apply(float t)
    {
        // 行き過ぎてから戻るカーブ（EaseOutBack）
        float u = t - 1f;
        float ease = 1f + (bounce + 1f) * u * u * u + bounce * u * u;

        rect.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, endScale, ease);

        // 出はじめの一瞬だけ薄い
        if (group) group.alpha = Mathf.Clamp01(t * 4f);
    }
}
