public class MagnetItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.MagnetUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string PopupLabel => "MAGNET UP!";

    public override int GetPopupValue(PlayerStatus playerStatus)
    {
        return playerStatus.MAGNET_POWER;
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(0.9f, 0.4f, 1f);
}
