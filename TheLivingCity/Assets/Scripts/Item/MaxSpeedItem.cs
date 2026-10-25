public class MaxSpeedItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.MaxSpeedUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);

    }

    public override string PopupLabel => "MAX SPEED UP!";

    public override int GetPopupValue(PlayerStatus playerStatus)
    {
        return playerStatus.MAX_SPEED;
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(1f, 0.3f, 0.3f);
}
