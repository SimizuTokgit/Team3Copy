using System.Diagnostics;

public class AcceleratorItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.AcceleratorUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string GetPopupText(PlayerStatus playerStatus)
    {
        return $"ACCEL UP!  <size=70%>{playerStatus.ACCELERATOR_POWER}</size>";
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(1f, 0.6f, 0.2f);
}
