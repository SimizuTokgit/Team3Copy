public class TurningItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.TurningUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string PopupLabel => "HANDLING UP!";

    public override float GetPopupValue(PlayerStatus playerStatus)
    {
        return playerStatus.TURNING_POWER;
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(1f, 1f, 0.4f);
}
