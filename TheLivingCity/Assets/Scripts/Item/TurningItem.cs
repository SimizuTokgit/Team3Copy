public class TurningItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.TurningUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }
}
