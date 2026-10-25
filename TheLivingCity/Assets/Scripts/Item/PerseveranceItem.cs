public class PerseveranceItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.PerseveranceUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }

    public override string PopupLabel => "DEFENSE UP!";

    public override int GetPopupValue(PlayerStatus playerStatus)
    {
        return playerStatus.PERSEVERANCE_POWER;
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(0.4f, 1f, 0.5f);
}
