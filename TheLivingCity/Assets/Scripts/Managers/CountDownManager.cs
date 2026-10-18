using UnityEngine;

//安井
//カウントダウン処理
public class CountDownManager : MonoBehaviour
{
    [SerializeField] private NumberImage countdownNumber; // カウントダウン表示
    [SerializeField] private GameObject goImage;          // スタート表示

    //清水 ここからSEと演出
    [Header("SE")]
    [SerializeField] private AudioClip startSE;

    //清水 1秒ずつではなく、StartSE.mp3 の波形から測った「音が鳴る秒」で切り替える
    //ピッの間隔は約0.87秒で、1秒刻みだとだんだん音に置いていかれていた
    [Header("タイミング（SEが鳴りはじめてからの秒）")]
    [SerializeField] private float startDelay = 0.6f;  //シーンのFadeIn(0.67秒)が明けるのを待ってから始める
    [SerializeField] private float count3Time = 0.00f; //1回目のピッ
    [SerializeField] private float count2Time = 0.88f; //2回目のピッ
    [SerializeField] private float count1Time = 1.74f; //3回目のピッ
    [SerializeField] private float goTime = 2.59f;     //ピーー（GO）
    [SerializeField] private float finishTime = 3.59f; //GOを消してゲーム開始。音が鳴り止むのは3.90秒
    [SerializeField] private float cueOffset = 0f;     //全体の微調整。＋にすると見た目が遅れる

    [Header("演出")]
    //小さい状態からドンと大きく出す。付いていなければ Start で自動で付ける
    [SerializeField] private CountDownPop numberPop; //3,2,1 用
    [SerializeField] private CountDownPop goPop;     //GO 用

    //4=まだ何も出さない / 3,2,1 / 0=GO / -1=終了
    private int _count = 4;

    private float _waitTime = 0f;     //startDelay を数える
    private bool _isStarted = false;

    //清水 音と同じ時計（AudioSettings.dspTime）で進めるので、処理落ちしても音とずれない
    private bool _useAudioClock = false;
    private double _startDspTime;
    private float _elapsedNoAudio = 0f; //SoundManagerがいないときだけ使う

    void Start()
    {
        //清水 演出が付いていなければ自動で付ける。GOのほうを小さく出して大きく行き過ぎさせる
        if (!numberPop && countdownNumber) numberPop = GetOrAddPop(countdownNumber.gameObject, 0.2f, 0.25f, 2.5f, false);
        if (!goPop && goImage) goPop = GetOrAddPop(goImage, 0.1f, 0.35f, 3.5f, true);

        //清水 最初のピッまでは何も出さない
        countdownNumber.gameObject.SetActive(false);
        goImage.SetActive(false);
    }

    /// <summary>
    /// カウントダウン処理
    /// </summary>
    public void CountDown()
    {
        //清水 フェードインが明けるまで待つ
        if (!_isStarted)
        {
            _waitTime += Time.deltaTime;
            if (_waitTime < startDelay) return;
            StartCount();
        }

        if (!_useAudioClock) _elapsedNoAudio += Time.deltaTime;

        int next = CountAt(GetElapsed() - cueOffset);
        if (next != _count) ChangeCount(next);
    }

    //清水 SEを鳴らして、同じ瞬間から時計を回しはじめる
    private void StartCount()
    {
        _isStarted = true;

        _useAudioClock = startSE && SoundManager.Instance;
        if (_useAudioClock)
        {
            SoundManager.Instance.PlayAudioClip(startSE);
            _startDspTime = AudioSettings.dspTime;
        }
    }

    private float GetElapsed()
    {
        if (_useAudioClock) return (float)(AudioSettings.dspTime - _startDspTime);
        return _elapsedNoAudio;
    }

    //清水 その秒に出すべきもの
    private int CountAt(float time)
    {
        if (time < count3Time) return 4;
        if (time < count2Time) return 3;
        if (time < count1Time) return 2;
        if (time < goTime) return 1;
        if (time < finishTime) return 0;
        return -1;
    }

    private void ChangeCount(int next)
    {
        _count = next;

        if (_count >= 1 && _count <= 3)
        {
            countdownNumber.gameObject.SetActive(true);
            countdownNumber.SetNumber(_count);
            PlayPop();
        }
        else if (_count == 0)
        {
            countdownNumber.gameObject.SetActive(false);
            goImage.SetActive(true); //GO側はCountDownPopのOnEnableで勝手に動く
        }
        else if (_count < 0)
        {
            countdownNumber.gameObject.SetActive(false);
            goImage.SetActive(false);
        }
    }

    //清水 自分で付けてあればその設定を使う。無ければ付けて強さを決める
    private CountDownPop GetOrAddPop(GameObject target, float startScale, float duration, float bounce, bool pulse)
    {
        CountDownPop pop = target.GetComponent<CountDownPop>();
        if (pop) return pop;

        pop = target.AddComponent<CountDownPop>();
        pop.Setup(startScale, duration, bounce, pulse);
        return pop;
    }

    //清水 数字を出し直したときの演出
    private void PlayPop()
    {
        if (numberPop) numberPop.Play();
    }

    /// <summary>
    /// カウントダウンが終了したか
    /// </summary>
    public bool CountFinishJudge()
    {
        return _count < 0 ;
    }
}
