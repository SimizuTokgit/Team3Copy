using UnityEngine;
using UnityEngine.InputSystem;

// Option画面の音量バー。点いている段数がそのまま音量になる
public class SoundBarManager : MonoBehaviour
{
    [SerializeField] private GameObject[] uiObjects;

    private const string VOLUME_KEY = "MasterVolume"; // SoundManagerが保存しているキー

    private const float DEADZONE = 0.3f;

    // 全部消えている状態を -1 として扱う（-1 は消音）
    private int currentIndex = -1;

    // 倒しっぱなしで連続して動かないようにするフラグ
    private bool horizontalInputUsed = false;

    private void Start()
    {
        // バーの位置は音量から作る。音量とバーがずれないようにここだけを正とする
        SetUI(VolumeToIndex(PlayerPrefs.GetFloat(VOLUME_KEY, 1f)));
    }

    private void Update()
    {
        float horizontal = GetHorizontal();

        // 入力が戻ったら再度受け付ける
        if (Mathf.Abs(horizontal) < DEADZONE)
        {
            horizontalInputUsed = false;
            return;
        }

        if (horizontalInputUsed) return;

        horizontalInputUsed = true;

        int nextIndex = Mathf.Clamp(currentIndex + (int)Mathf.Sign(horizontal), -1, uiObjects.Length - 1);
        if (nextIndex != currentIndex) OnButtonClick(nextIndex);
    }

    private float GetHorizontal()
    {
        // A / D、←/→、左スティック
        float horizontal = Input.GetAxisRaw("Horizontal");

        // 十字キーは Horizontal軸に含まれていないので個別に取る
        if (Gamepad.current != null) horizontal += Gamepad.current.dpad.x.ReadValue();

        return horizontal;
    }

    // バーのボタンを押したとき / パッドで動かしたとき
    public void OnButtonClick(int buttonIndex)
    {
        SetUI(buttonIndex);

        // SoundManager側でPlayerPrefsへの保存までやってくれる
        if (SoundManager.Instance) SoundManager.Instance.SetMasterVolume(IndexToVolume(buttonIndex));
    }

    private void SetUI(int buttonIndex)
    {
        currentIndex = buttonIndex;

        for (int i = 0; i < uiObjects.Length; i++)
        {
            uiObjects[i].SetActive(i <= buttonIndex);
        }
    }

    // 右端まで点いていると 1.0、全部消えていると 0
    private float IndexToVolume(int buttonIndex)
    {
        if (uiObjects.Length <= 0) return 1f;
        return (buttonIndex + 1) / (float)uiObjects.Length;
    }

    private int VolumeToIndex(float volume)
    {
        if (uiObjects.Length <= 0) return -1;
        int index = Mathf.RoundToInt(volume * uiObjects.Length) - 1;
        return Mathf.Clamp(index, -1, uiObjects.Length - 1);
    }
}
