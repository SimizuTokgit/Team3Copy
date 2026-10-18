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

    void Start()
    {
        playerStatus = FindFirstObjectByType<Player>().PlayerStatus;

        playerStatus.GetMaxSpeedItem += MaxSpeedUp;   // çwì«äJén
        playerStatus.GetAcceleratorPowerItem += AcceleratorUp;
        playerStatus.GetPerseverancePowerItem += PerseveranceUp;
        playerStatus.GetBoostPowerItem += BoostUp;
        playerStatus.GetMagnetPowerItem += MagnetUp;
        playerStatus.GetTurningPowerItem += TurnUp;

        MaxSpeedMask.Init(playerStatus.MAX_SPEED_LIMIT);

    }
    void OnDestroy()
    {
        if (playerStatus != null)
        {
            playerStatus.GetMaxSpeedItem -= MaxSpeedUp;   // çwì«âèú
            playerStatus.GetAcceleratorPowerItem -= AcceleratorUp;
            playerStatus.GetPerseverancePowerItem -= PerseveranceUp;
            playerStatus.GetBoostPowerItem -= BoostUp;
            playerStatus.GetMagnetPowerItem -= MagnetUp;
            playerStatus.GetTurningPowerItem -= TurnUp;
        }
    }

    private void MaxSpeedUp()
    {
        MaxSpeedMask.GetItem();
    }

    private void AcceleratorUp()
    {
        AcceleratorMask.GetItem();
    }

    private void PerseveranceUp()
    {
        PerseveranceMask.GetItem();
    }

    private void BoostUp()
    {
        BoostMask.GetItem();
    }

    private void MagnetUp()
    {
        MagnetMask.GetItem();
    }

    private void TurnUp()
    {
        TurningMask.GetItem();
    }
}
