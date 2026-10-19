using UnityEngine;

//安井
//音量調節
public class VolumeButton : MonoBehaviour
{
    //音量
    [SerializeField] private int volumePercent = 100;

    //音量設定
    public void SetVolume()
    {
        SoundManager.Instance.SetMasterVolume(volumePercent / 100f);
    }
}