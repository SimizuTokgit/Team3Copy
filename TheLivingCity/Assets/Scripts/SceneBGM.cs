using UnityEngine;

// シーンが始まったらBGMを切り替える
public class SceneBGM : MonoBehaviour
{
    [SerializeField] private AudioClip bgm; // 空にするとBGMを止める

    private void Start()
    {
        if (SoundManager.Instance) SoundManager.Instance.ChangeBGM(bgm);
    }
}
