using System;
using UnityEngine;

public class PlayerStatus
{
    // 変更通知用イベント
    public event Action GetMaxSpeedItem;
    public event Action GetAcceleratorPowerItem;
    public event Action GetPerseverancePowerItem;
    public event Action GetBoostPowerItem;
    public event Action GetMagnetPowerItem;
    public event Action GetTurningPowerItem;

    public int MAX_SPEED { get; private set; }              //最高速度
    public int ACCELERATOR_POWER { get; private set; }      //加速力
    public int PERSEVERANCE_POWER { get; private set; }     //忍耐力
    public int BOOST_POWER { get; private set; }            //ブースト力
    public int MAGNET_POWER { get; private set; }           //アイテム回収力
    public int TURNING_POWER { get; private set; }          //小回り力


    public int MAX_SPEED_LIMIT { get; private set; } = 80;
    private readonly int ACCELERATOR_POWER_LIMIT = 20;
    private readonly int PERSEVERANCE_POWER_LIMIT = 30;
    private readonly int BOOST_POWER_LIMIT = 10;
    private readonly int MAGNET_POWER_LIMIT = 30;
    private readonly int TURNING_POWER_LIMIT = 15;


    //コンストラクタ(初期化関数)
    public PlayerStatus()
    {
        MAX_SPEED = 40;
        ACCELERATOR_POWER = 10;
        PERSEVERANCE_POWER = 20;
        BOOST_POWER = 1;
        MAGNET_POWER = 10;
        TURNING_POWER = 10;
    }

    //清水 最高速度は上限なしで上がり続ける。走っているときの速さ（ここまで加速する）と、ブースト後に戻る速さを決める
    //MAX_SPEED_LIMIT はスピードメーターの目盛り（1周分）として使う
    public void MaxSpeedUpGrade(int speed)
    {
        MAX_SPEED += speed;

        GetMaxSpeedItem?.Invoke();   // ← 変化を通知
    }

    public void AcceleratorUpGrade(int accel)
    {
        if (ACCELERATOR_POWER >= ACCELERATOR_POWER_LIMIT)
        {
            Debug.Log("加速力はこれ以上上がらない");
            ACCELERATOR_POWER = ACCELERATOR_POWER_LIMIT;
            return;
        }
        ACCELERATOR_POWER = Mathf.Min(ACCELERATOR_POWER + accel, ACCELERATOR_POWER_LIMIT);

        GetAcceleratorPowerItem?.Invoke();
    }

    public void PerseveranceUpGrade(int perseverance)
    {
        if (PERSEVERANCE_POWER >= PERSEVERANCE_POWER_LIMIT)
        {
            Debug.Log("忍耐力はこれ以上上がらない");
            PERSEVERANCE_POWER = PERSEVERANCE_POWER_LIMIT;
            return;
        }
        PERSEVERANCE_POWER = Mathf.Min(PERSEVERANCE_POWER + perseverance, PERSEVERANCE_POWER_LIMIT);

        GetPerseverancePowerItem?.Invoke();
    }

    public void BoostUpGrade(int boost)
    {
        if (BOOST_POWER >= BOOST_POWER_LIMIT)
        {
            Debug.Log("ブースト力はこれ以上上がらない");
            BOOST_POWER = BOOST_POWER_LIMIT;
            return;
        }
        BOOST_POWER = Mathf.Min(BOOST_POWER + boost, BOOST_POWER_LIMIT);

        GetBoostPowerItem?.Invoke();
    }

    public void MagnetUpGrade(int magnet)
    {
        if (MAGNET_POWER >= MAGNET_POWER_LIMIT)
        {
            Debug.Log("マグネットはこれ以上上がらない");
            MAGNET_POWER = MAGNET_POWER_LIMIT;
            return;
        }
        MAGNET_POWER = Mathf.Min(MAGNET_POWER + magnet, MAGNET_POWER_LIMIT);

        GetMagnetPowerItem?.Invoke();
    }

    public void TurningUpGrade(int turning)
    {
        if (TURNING_POWER >= TURNING_POWER_LIMIT)
        {
            Debug.Log("旋回力はこれ以上上がらない");
            TURNING_POWER = TURNING_POWER_LIMIT;
            return;
        }
        TURNING_POWER = Mathf.Min(TURNING_POWER + turning, TURNING_POWER_LIMIT);

        GetTurningPowerItem?.Invoke();
    }

    public int GetMaxSpeedLimit()
    {
        return MAX_SPEED_LIMIT;
    }
}
