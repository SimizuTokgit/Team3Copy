public class BoostItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.BoostUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);

    }

    public override string PopupLabel => "BOOST UP!";

    public override int GetPopupValue(PlayerStatus playerStatus)
    {
        return playerStatus.BOOST_POWER;
    }

    public override UnityEngine.Color PopupColor => new UnityEngine.Color(0.3f, 0.8f, 1f);
}
