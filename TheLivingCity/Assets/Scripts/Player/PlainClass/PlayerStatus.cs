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

    public float MAX_SPEED { get; private set; }              //最高速度
    public float ACCELERATOR_POWER { get; private set; }      //加速力
    public float PERSEVERANCE_POWER { get; private set; }     //忍耐力
    public float BOOST_POWER { get; private set; }            //ブースト力
    public float MAGNET_POWER { get; private set; }           //アイテム回収力
    public float TURNING_POWER { get; private set; }          //小回り力


    public int MAX_SPEED_LIMIT { get; private set; } = 80;

    //清水 ステータスは float なので、アイテムで 0.5 ずつのように細かく上げられる

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

    //清水 ステータスはどれも上限なしで上がり続ける
    //最高速度は走っているときの速さ（ここまで加速する）と、ブースト後に戻る速さを決める
    //MAX_SPEED_LIMIT はスピードメーターの目盛り（1周分）として使う
    public void MaxSpeedUpGrade(float speed)
    {
        MAX_SPEED += speed;

        GetMaxSpeedItem?.Invoke();   // ← 変化を通知
    }

    public void AcceleratorUpGrade(float accel)
    {
        ACCELERATOR_POWER += accel;

        GetAcceleratorPowerItem?.Invoke();
    }

    public void PerseveranceUpGrade(float perseverance)
    {
        PERSEVERANCE_POWER += perseverance;

        GetPerseverancePowerItem?.Invoke();
    }

    public void BoostUpGrade(float boost)
    {
        BOOST_POWER += boost;

        GetBoostPowerItem?.Invoke();
    }

    public void MagnetUpGrade(float magnet)
    {
        MAGNET_POWER += magnet;

        GetMagnetPowerItem?.Invoke();
    }

    public void TurningUpGrade(float turning)
    {
        TURNING_POWER += turning;

        GetTurningPowerItem?.Invoke();
    }

    public int GetMaxSpeedLimit()
    {
        return MAX_SPEED_LIMIT;
    }
}
