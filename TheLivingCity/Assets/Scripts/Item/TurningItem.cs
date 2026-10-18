public class TurningItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.TurningUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string GetPopupText(PlayerStatus playerStatus)
    {
        return $"HANDLING UP!  <size=70%>{playerStatus.TURNING_POWER}</size>";
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(1f, 1f, 0.4f);
}
