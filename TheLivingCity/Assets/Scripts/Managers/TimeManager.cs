using UnityEngine;

//安井
//制限時間を処理
public class TimeManager : MonoBehaviour
{
    [SerializeField] private float TIME_LIMIT;         // 制限時間
    [SerializeField] private float TIME_RECOVERY;      // 回復時間
    [SerializeField] private float LOW_TIME_THRESHOLD; // 残り時間が少ない判定

    [SerializeField] private NumberImage REMAINING_TIME_NUMBER; // 制限時間表示
    public float _remainingTime; //残り時間

    //清水 チェックポイントで回復する秒数（UI に出す用）
    public float RecoveryTime => TIME_RECOVERY;

    void Start()
    {
        _remainingTime = TIME_LIMIT;
        //清水 小数も出せるように float のまま渡す（桁数は NumberImage の Decimals で決める）
        REMAINING_TIME_NUMBER.SetNumber(_remainingTime);
    }

    /// <summary>
    /// 残り時間を減らす・表示更新
    /// </summary>
    public void RemainingTime()
    {
        _remainingTime -= Time.deltaTime;
        REMAINING_TIME_NUMBER.SetWarning(IsLowTime());
        //清水 小数も出せるように float のまま渡す
        REMAINING_TIME_NUMBER.SetNumber(_remainingTime);
    }

    /// <summary>
    /// 残り時間を増やす
    /// </summary>
    public void RecoverTime()
    {
        _remainingTime += TIME_RECOVERY;
    }

    /// <summary>
    /// 清水 指定した秒数だけ残り時間を増やす（ボス撃破ボーナスなど）
    /// </summary>
    public void AddTime(float seconds)
    {
        _remainingTime += seconds;
    }

    /// <summary>
    /// 残り時間の有無を判断する
    /// </summary>
    public bool IsTimeUp()
    {
        return _remainingTime <= 0.0f;
    }

    /// <summary>
    /// 残り時間が少ないかどうかを判定する
    /// </summary>
    public bool IsLowTime()
    {
        return _remainingTime <= LOW_TIME_THRESHOLD;
    }

}
