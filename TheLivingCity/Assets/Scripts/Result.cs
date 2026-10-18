using UnityEngine;

//安井
//Resultシーンの処理
public class Result : MonoBehaviour
{
    [SerializeField] private NumberImage ScoreNumber;

    void Start()
    {
        //スコア取得&表示
        float score = PlayerPrefs.GetFloat("Score", 0f);
        ScoreNumber.SetNumber(Mathf.FloorToInt(score));
    }
}
