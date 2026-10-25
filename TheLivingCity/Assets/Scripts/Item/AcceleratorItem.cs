using System.Diagnostics;

public class AcceleratorItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.AcceleratorUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string PopupLabel => "ACCEL UP!";

    public override float GetPopupValue(PlayerStatus playerStatus)
    {
        return playerStatus.ACCELERATOR_POWER;
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(1f, 0.6f, 0.2f);
}
