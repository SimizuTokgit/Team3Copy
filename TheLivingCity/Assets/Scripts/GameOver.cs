using TMPro;
using UnityEngine;

//安井
//ゲームオーバーシーン
public class GameOver : MonoBehaviour
{
    [SerializeField] private GameObject Explosion;
    [SerializeField] private GameObject Fade;

    [SerializeField] private GameObject Texts;
    [SerializeField] private GameObject Noise;

    [SerializeField] private TextMeshProUGUI Text;
    [SerializeField] private NumberImage ScoreNumber;

    //清水 最高スコア表示。未設定なら何もしない
    [SerializeField] private NumberImage HighScoreNumber;
    [SerializeField] private GameObject NewRecordObject;

    //清水 ここからSE
    [Header("SE")]
    [SerializeField] private AudioClip alertSE;     //爆発前に鳴らす警告音
    [SerializeField] private AudioClip explosionSE; //爆発の瞬間に鳴らす音
    [SerializeField] private AudioClip scoreTickSE; //スコアが1桁止まるたびに鳴らす音

    //爆発は GameOver.anim のイベントで 3.5 秒のところに入っている
    [SerializeField] private float alertStartTime = 0.2f; //シーン開始から何秒で警告音を鳴らすか

    //警告音は爆発の瞬間に止めたいので、SoundManagerではなく専用のAudioSourceで鳴らす
    //未設定なら実行時に自分で足す。OutputにSEのミキサーグループを入れるとOptionの音量が効く
    [SerializeField] private AudioSource alertSource;

    private bool isPushButton = false;

    private bool isScoreShown = false;

    void Start()
    {
        ShowScore();

        //爆発音はアニメーションイベントで鳴らすので、ここでは警告音だけ予約する
        if (alertSE) Invoke(nameof(PlayAlertSE), Mathf.Max(alertStartTime, 0f));
    }

    /// <summary>
    /// 清水 スコアの表示と Yes / No の操作の準備。
    /// 電撃のシーンのように GameOver が非アクティブ（Start が来ない）でも、ほかの演出スクリプトから呼べるようにした
    /// 何回呼んでも1回だけ動く
    /// </summary>
    public void ShowScore()
    {
        if (isScoreShown) return;
        isScoreShown = true;

        //スコア取得&表示
        float score = PlayerPrefs.GetFloat("Score", 0f);
        if (ScoreNumber) ScoreNumber.SetNumber(Mathf.FloorToInt(score));

        if (HighScoreNumber) HighScoreNumber.SetNumber(Mathf.FloorToInt(PlayerPrefs.GetFloat(PlayModeInfo.HighScoreKey, 0f)));
        if (NewRecordObject) NewRecordObject.SetActive(PlayerPrefs.GetInt("IsNewRecord", 0) == 1);

        //清水 スコアを一の位から一桁ずつ出す。付いていなければ自動で付ける
        //ScoreNumber は Texts の下なので、Texts が出た瞬間（GameOver.anim の5.0秒）に始まる
        if (ScoreNumber)
        {
            NumberReveal reveal = ScoreNumber.GetComponent<NumberReveal>();
            if (!reveal) reveal = ScoreNumber.gameObject.AddComponent<NumberReveal>();
            reveal.SetSE(scoreTickSE, null);
        }

        //清水 Yes / No を A・D / 左スティック / 十字キーで選べるようにする（Texts が出たときに Yes を選ぶ）
        if (Texts) MenuNavigation.Attach(Texts, MenuNavigation.Direction.Horizontal, true, null);
    }

    public void MainButton()
    {
        if (!isPushButton)
        {
            //清水 遊んでいたモードのシーンでやり直す
            FindFirstObjectByType<FadeImageController>().SceneChange(PlayModeInfo.GameSceneName);
            isPushButton = true;
        }
    }

    public void TitleButton()
    {
        if (!isPushButton)
        {
            FindFirstObjectByType<FadeImageController>().SceneChange("Title");
            isPushButton = true;
        }
    }

    private void ExplosionTrue()
    {
        Explosion.SetActive(true);

        //清水 警告音を止めてから爆発音を鳴らす
        StopAlertSE();
        PlaySE(explosionSE);
    }

    private void ExplosionFalse()
    {
        Explosion.SetActive(false);
    }

    private void FadeFalse()
    {
        Fade.SetActive(false);
    }

    private void TextGeneret()
    {
        Texts.SetActive(true);
    }

    private void NoiseGeneret()
    {
        Noise.SetActive(true);
    }

    //清水 Startから予約して鳴らす
    private void PlayAlertSE()
    {
        if (!alertSE) return;

        if (!alertSource)
        {
            alertSource = gameObject.AddComponent<AudioSource>();
            alertSource.playOnAwake = false;
        }

        alertSource.clip = alertSE;
        alertSource.Play();
    }

    private void StopAlertSE()
    {
        if (alertSource && alertSource.isPlaying) alertSource.Stop();
    }

    private void PlaySE(AudioClip clip)
    {
        if (clip && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(clip);
    }
}
