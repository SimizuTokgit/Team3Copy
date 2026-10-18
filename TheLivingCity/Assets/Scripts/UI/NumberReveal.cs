using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// NumberImage の数字を、一の位から左へ一桁ずつ「回って → ガチッと止まる」で出す
// 表示された瞬間（SetActive / OnEnable）に始まる。値は先に NumberImage.SetNumber で入れておく
[RequireComponent(typeof(NumberImage))]
public class NumberReveal : MonoBehaviour
{
    [Header("タイミング")]
    [SerializeField] private float startDelay = 0.2f;     // 表示されてから回しはじめるまで
    [SerializeField] private float rollTime = 0.22f;      // 1桁が回っている時間
    [SerializeField] private float rollInterval = 0.035f; // 回っている間に数字を変える間隔
    [SerializeField] private float maxTotalTime = 2.0f;   // 桁が多くても全体でこれ以上はかけない

    [Header("見た目")]
    [SerializeField] private bool rollHiddenDigits = false; // まだの桁も回しておく（スロット風）。falseなら空白のまま
    [SerializeField] private float rollingAlpha = 0.5f;     // 回っている桁の濃さ。止まった桁は1
    [SerializeField] private float popScale = 1.35f;        // 止まった瞬間に一度大きくする
    [SerializeField] private float popTime = 0.14f;

    [Header("SE")]
    [SerializeField] private AudioClip tickSE;   // 1桁止まるたびに鳴らす
    [SerializeField] private AudioClip finishSE; // 全部そろったときに鳴らす

    private NumberImage number;
    private string target;     // 完成形
    private Coroutine routine;

    public bool IsFinished { get; private set; }

    // 外から自動で付けたときにSEを渡す。nullのものは今の設定のまま
    public void SetSE(AudioClip tick, AudioClip finish)
    {
        if (tick) tickSE = tick;
        if (finish) finishSE = finish;
    }

    private void Awake()
    {
        number = GetComponent<NumberImage>();
    }

    private void OnEnable()
    {
        IsFinished = false;

        // すでに値が入っていれば、この瞬間に隠す（1フレームでも完成形が見えないように）
        target = number.CurrentText;
        if (!string.IsNullOrEmpty(target)) number.ShowRawText(new string(' ', target.Length));

        routine = StartCoroutine(Reveal());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        // 途中で消されたら完成形に戻しておく
        if (!string.IsNullOrEmpty(target)) number.ShowRawText(target);
    }

    private IEnumerator Reveal()
    {
        // 値がまだ入っていなければ、入るまで待つ
        while (string.IsNullOrEmpty(target))
        {
            yield return null;
            target = number.CurrentText;
            if (!string.IsNullOrEmpty(target)) number.ShowRawText(new string(' ', target.Length));
        }

        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        // 桁が多いときは1桁あたりを短くして、全体の長さを抑える
        int digitCount = 0;
        foreach (char c in target) if (char.IsDigit(c)) digitCount++;
        float perDigit = rollTime;
        if (digitCount > 0 && perDigit * digitCount > maxTotalTime) perDigit = maxTotalTime / digitCount;

        char[] shown = new string(' ', target.Length).ToCharArray();

        // 一の位（右端）から左へ
        for (int i = target.Length - 1; i >= 0; i--)
        {
            // 小数点などは回さずにそのまま出す
            if (!char.IsDigit(target[i]))
            {
                shown[i] = target[i];
                continue;
            }

            // この桁を回す
            float time = 0f;
            while (time < perDigit)
            {
                shown[i] = RandomDigit();
                if (rollHiddenDigits) FillHidden(shown, i);
                Show(shown, i);

                yield return new WaitForSeconds(rollInterval);
                time += rollInterval;
            }

            // 止める
            shown[i] = target[i];
            if (rollHiddenDigits) FillHidden(shown, i);
            Show(shown, -1);

            StartCoroutine(Pop(number.GetDigitImage(i)));
            PlaySE(tickSE);
        }

        number.ShowRawText(target);
        PlaySE(finishSE);

        IsFinished = true;
        routine = null;
    }

    // rollingIndex の桁（と、まだの桁）だけ薄くする
    private void Show(char[] shown, int rollingIndex)
    {
        number.ShowRawText(new string(shown));

        if (rollingAlpha >= 1f) return;

        int from = rollHiddenDigits ? 0 : rollingIndex;
        for (int i = Mathf.Max(from, 0); i <= rollingIndex; i++)
        {
            Image image = number.GetDigitImage(i);
            if (!image) continue;

            Color color = image.color;
            color.a = rollingAlpha;
            image.color = color;
        }
    }

    // まだ止めていない桁（index より左）をでたらめな数字にする
    private void FillHidden(char[] shown, int index)
    {
        for (int i = 0; i < index; i++)
        {
            if (char.IsDigit(target[i])) shown[i] = RandomDigit();
        }
    }

    private char RandomDigit()
    {
        return (char)('0' + Random.Range(0, 10));
    }

    // 止まった瞬間だけ大きくして、すぐ元に戻す
    private IEnumerator Pop(Image image)
    {
        if (!image) yield break;

        RectTransform rect = image.rectTransform;
        float time = 0f;

        while (time < popTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / popTime);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            rect.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, ease);
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    private void PlaySE(AudioClip clip)
    {
        if (clip && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(clip);
    }
}
