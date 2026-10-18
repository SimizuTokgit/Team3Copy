using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    void Start()
    {
        //スコア取得&表示
        float score = PlayerPrefs.GetFloat("Score", 0f);
        ScoreNumber.SetNumber(Mathf.FloorToInt(score));

        //清水 スコアを一の位から一桁ずつ出す。付いていなければ自動で付ける
        //ScoreNumber は Texts の下なので、Texts が出た瞬間（GameOver.anim の5.0秒）に始まる
        if (ScoreNumber)
        {
            NumberReveal reveal = ScoreNumber.GetComponent<NumberReveal>();
            if (!reveal) reveal = ScoreNumber.gameObject.AddComponent<NumberReveal>();
            reveal.SetSE(scoreTickSE, null);
        }

        //爆発音はアニメーションイベントで鳴らすので、ここでは警告音だけ予約する
        if (alertSE) Invoke(nameof(PlayAlertSE), Mathf.Max(alertStartTime, 0f));
    }

    public void MainButton()
    {
        if (!isPushButton)
        {
            FindFirstObjectByType<FadeImageController>().SceneChange("Main");
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
