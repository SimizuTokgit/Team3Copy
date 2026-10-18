public class PerseveranceItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.PerseveranceUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }
}
