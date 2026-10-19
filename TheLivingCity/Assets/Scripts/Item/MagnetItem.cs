public class MagnetItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.MagnetUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string GetPopupText(PlayerStatus playerStatus)
    {
        return $"MAGNET UP!  <size=70%>{playerStatus.MAGNET_POWER}</size>";
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(0.9f, 0.4f, 1f);
}
