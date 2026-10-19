public class BoostItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.BoostUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);

    }

    public override string GetPopupText(PlayerStatus playerStatus)
    {
        return $"BOOST UP!  <size=70%>{playerStatus.BOOST_POWER}</size>";
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(0.3f, 0.8f, 1f);
}
