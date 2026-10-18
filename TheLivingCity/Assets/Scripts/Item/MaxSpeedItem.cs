public class MaxSpeedItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.MaxSpeedUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);

    }
}
