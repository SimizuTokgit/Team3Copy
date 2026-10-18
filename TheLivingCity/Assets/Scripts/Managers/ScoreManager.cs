using TMPro;
using UnityEngine;

//安井
//スコア計算処理
public class ScoreManager : MonoBehaviour
{
    private float SCORE;

    /// <summary>
    /// スコア計算 (距離、残り時間、時間内にゴールしたか)
    /// </summary>
    public void ScoreCalculation(float distance, float remainingTime, bool isGoal)
    {
        if (isGoal) SCORE = distance * remainingTime;
        else SCORE = distance;
    }

    /// <summary>
    /// スコア入力
    /// </summary>
    public void SetScore()
    {
        PlayerPrefs.SetFloat("Score", SCORE);

        //清水 モードごとの最高スコアを更新する
        bool isNewRecord = SCORE > PlayerPrefs.GetFloat(PlayModeInfo.HighScoreKey, 0f);
        if (isNewRecord) PlayerPrefs.SetFloat(PlayModeInfo.HighScoreKey, SCORE);
        PlayerPrefs.SetInt("IsNewRecord", isNewRecord ? 1 : 0);
        PlayerPrefs.Save();
    }
}