using UnityEngine;

//à¿à‰
//âπó í≤êﬂ
public class VolumeButton : MonoBehaviour
{
    //âπó 
    [SerializeField] private int volumePercent = 100;

    //âπó ê›íË
    public void SetVolume()
    {
        SoundManager.Instance.SetMasterVolume(volumePercent / 100f);
    }
}