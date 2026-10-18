public class MagnetItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.MagnetUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }
}
