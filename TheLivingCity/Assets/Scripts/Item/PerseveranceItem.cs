public class PerseveranceItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.PerseveranceUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string GetPopupText(PlayerStatus playerStatus)
    {
        return $"DEFENSE UP!  <size=70%>{playerStatus.PERSEVERANCE_POWER}</size>";
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(0.4f, 1f, 0.5f);
}
