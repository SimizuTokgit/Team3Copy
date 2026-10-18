using System.Diagnostics;

public class AcceleratorItem : Item
{
    public override void UpGrade(PlayerStatus playerStatus)
    {
        playerStatus.AcceleratorUpGrade(UP_NUMBER);
        SoundManager.Instance.PlayAudioClip(ItemSE);
        Destroy(gameObject);
    }
}
