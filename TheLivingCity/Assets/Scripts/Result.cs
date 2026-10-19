using UnityEngine;

//安井
//Resultシーンの処理
public class Result : MonoBehaviour
{
    [SerializeField] private NumberImage ScoreNumber;

    //清水 最高スコア表示。未設定なら何もしない
    [SerializeField] private NumberImage HighScoreNumber;
    [SerializeField] private GameObject NewRecordObject;

    void Start()
    {
        //スコア取得&表示
        float score = PlayerPrefs.GetFloat("Score", 0f);
        ScoreNumber.SetNumber(Mathf.FloorToInt(score));

        if (HighScoreNumber) HighScoreNumber.SetNumber(Mathf.FloorToInt(PlayerPrefs.GetFloat(PlayModeInfo.HighScoreKey, 0f)));
        if (NewRecordObject) NewRecordObject.SetActive(PlayerPrefs.GetInt("IsNewRecord", 0) == 1);
    }
}
