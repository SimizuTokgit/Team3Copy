public class BoostItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.BoostUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);

    }
}
