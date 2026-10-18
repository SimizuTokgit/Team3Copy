using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialState
    {
        Challenge,
        ChallengeDodge,
        Clear,
        Next,
        Finish,
    };
    public TutorialState tutorialstate;

    //チュートリアルのテキスト
    public Text tutorialText;

    //チュートリアルの内容
    [SerializeField]
    private string[] tutorialContents;

    //チュートリアル何番目か
    public int tutorialCount = 0;

    //チュートリアルクリアしたかどうか
    public bool tutorialClear;

    //次のチュートリアルに移るための時間
    [SerializeField]
    private float nexttutorialTime;
    private float elapsedTime;

    //最後障害物をよける　何秒避けるか
    [SerializeField]
    private float DodgeTime;
    //避けている時間
    private float dodgeTime;

    //チュートリアルすべて完了したかどうか
    public bool tutorialClearAll;

    //すべて完了したときに出すテキスト
    public Text FinishText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tutorialCount = 0;
        tutorialText.text = tutorialContents[tutorialCount];
        tutorialClear = false;
        tutorialClearAll = false;

        elapsedTime = 0;
        dodgeTime = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (tutorialstate == TutorialState.Challenge)
        {
            if (tutorialClear)
                tutorialstate = TutorialState.Clear;
        }
        else if (tutorialstate == TutorialState.ChallengeDodge)
        {
            dodgeTime += Time.deltaTime;

            tutorialText.text = tutorialContents[tutorialCount] + "(" + dodgeTime.ToString("F0") + "秒/" + DodgeTime.ToString("F0") + "秒)";

            if (DodgeTime <= dodgeTime)
            {
                ClearTutorial();
            }

            if (tutorialClear)
                tutorialstate = TutorialState.Clear;
        }
        else if (tutorialstate == TutorialState.Clear)
        {
            NextTutorial();
        }
        else if (tutorialstate == TutorialState.Next)
        {
            //次のチュートリアルまで何秒か待つ

            elapsedTime += Time.deltaTime;

            if (nexttutorialTime <= elapsedTime)
            {
                tutorialClear = false;

                tutorialText.text = tutorialContents[tutorialCount];

                if (tutorialCount == 2)
                    ItemGetTextNumberChange(0);

                if (tutorialCount == tutorialContents.Length - 1)
                {
                    tutorialstate = TutorialState.ChallengeDodge;
                    return;
                }

                tutorialstate = TutorialState.Challenge;
            }
        }
        else if (tutorialstate == TutorialState.Finish)
        {
            //全てのチュートリアルを完了したときの処理

            if(Input.GetKeyDown(KeyCode.Return))
            {
                //タイトルに戻る
                SceneManager.LoadScene("Title");
            }
        }
    }

    //次のチュートリアルに移動
    public void NextTutorial()
    {
        tutorialCount++;

        if (tutorialContents.Length == tutorialCount)
        {
            tutorialClearAll = true;
            Debug.Log("チュートリアル完了");

            tutorialText.text = "AllClear!";

            tutorialstate = TutorialState.Finish;

            FinishText.enabled = true;

            return;
        }

        tutorialText.text = "Clear!";

        elapsedTime = 0.0f;

        tutorialstate = TutorialState.Next;
    }

    //アイテムをゲットするチュートリアルアイテムゲット数
    public void ItemGetTextNumberChange(int number)
    {
        tutorialText.text = tutorialContents[tutorialCount] + "(" + number + "/5)";
    }

    //障害物を避けるチュートリアルで障害物に当たった時
    public void DodgeTimeReset()
    {
        dodgeTime = 0.0f;
    }

    //クリアした時に呼ぶ
    public void ClearTutorial()
    {
        tutorialClear = true;
    }
}
