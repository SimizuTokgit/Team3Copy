public class MaxSpeedItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.MaxSpeedUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);

    }

    public override string GetPopupText(PlayerStatus playerStatus)
    {
        return $"MAX SPEED UP!  <size=70%>{playerStatus.MAX_SPEED}</size>";
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(1f, 0.3f, 0.3f);
}
