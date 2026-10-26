using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] StatusUIMask MaxSpeedMask;
    [SerializeField] StatusUIMask AcceleratorMask;
    [SerializeField] StatusUIMask BoostMask;
    [SerializeField] StatusUIMask PerseveranceMask;
    [SerializeField] StatusUIMask TurningMask;
    [SerializeField] StatusUIMask MagnetMask;

    private PlayerStatus playerStatus;

    [SerializeField] float AddAmountValue;

    void Start()
    {
        playerStatus = FindFirstObjectByType<Player>().PlayerStatus;

        playerStatus.GetMaxSpeedItem += MaxSpeedUp;   // 購読開始
        playerStatus.GetAcceleratorPowerItem += AcceleratorUp;
        playerStatus.GetPerseverancePowerItem += PerseveranceUp;
        playerStatus.GetBoostPowerItem += BoostUp;
        playerStatus.GetMagnetPowerItem += MagnetUp;
        playerStatus.GetTurningPowerItem += TurnUp;

        //MaxSpeedMask.Init(playerStatus.MAX_SPEED_LIMIT);
        //AcceleratorMask.Init(playerStatus.ACCELERATOR_POWER_LIMIT);
        //PerseveranceMask.Init(playerStatus.PERSEVERANCE_POWER_LIMIT);
        //BoostMask.Init(playerStatus.BOOST_POWER_LIMIT);
        //MagnetMask.Init(playerStatus.MAGNET_POWER_LIMIT);
        //TurningMask.Init(playerStatus.TURNING_POWER_LIMIT);

    }
    void OnDestroy()
    {
        if (playerStatus != null)
        {
            playerStatus.GetMaxSpeedItem -= MaxSpeedUp;   // 購読解除
            playerStatus.GetAcceleratorPowerItem -= AcceleratorUp;
            playerStatus.GetPerseverancePowerItem -= PerseveranceUp;
            playerStatus.GetBoostPowerItem -= BoostUp;
            playerStatus.GetMagnetPowerItem -= MagnetUp;
            playerStatus.GetTurningPowerItem -= TurnUp;
        }
    }

    private void MaxSpeedUp()
    {
        MaxSpeedMask.GetItem(AddAmountValue);
    }

    private void AcceleratorUp()
    {
        AcceleratorMask.GetItem(AddAmountValue);
    }

    private void PerseveranceUp()
    {
        PerseveranceMask.GetItem(AddAmountValue);
    }

    private void BoostUp()
    {
        BoostMask.GetItem(AddAmountValue);
    }

    private void MagnetUp()
    {
        MagnetMask.GetItem(AddAmountValue);
    }

    private void TurnUp()
    {
        TurningMask.GetItem(AddAmountValue);
    }
}
