using UnityEngine;
using UnityEngine.EventSystems;

public class TitleManager : MonoBehaviour
{
    private bool isPushButton = false;

    [SerializeField] private GameObject[] modeSelectUIs;
    [SerializeField] private GameObject[] titleUIs;

    [SerializeField] private GameObject titleFirstButton;      // タイトルで最初に選ぶボタン
    [SerializeField] private GameObject modeSelectFirstButton; // モード選択で最初に選ぶボタン

    private void Start()
    {
        SetModeSelect(false);
    }

    public void StartButton()
    {
        if (!isPushButton)
        {
            SetModeSelect(true);
        }
    }

    public void BackButton()
    {
        if (!isPushButton)
        {
            SetModeSelect(false);
        }
    }

    // タイトルとモード選択の切り替え
    private void SetModeSelect(bool isModeSelect)
    {
        for (int i = 0; i < titleUIs.Length; i++)
        {
            titleUIs[i].SetActive(!isModeSelect);
        }
        for (int i = 0; i < modeSelectUIs.Length; i++)
        {
            modeSelectUIs[i].SetActive(isModeSelect);
        }

        // パッドでも操作できるように選択中のボタンを移す
        if (EventSystem.current)
        {
            EventSystem.current.SetSelectedGameObject(isModeSelect ? modeSelectFirstButton : titleFirstButton);
        }
    }

    public void ExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OptionButton()
    {
        ChangeScene("Option");
    }

    public void StoryMode()
    {
        ChangeScene("StoryGameScene");
    }

    public void EndlesMode()
    {
        ChangeScene("Main");
    }

    public void TutorialMode()
    {
        ChangeScene("Tutorial");
    }

    private void ChangeScene(string sceneName)
    {
        if (isPushButton) return;

        // Build Settingsに無いシーンは読み込めないので何もしない
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(sceneName + " がBuild Settingsに入っていません");
            return;
        }

        FindFirstObjectByType<FadeImageController>().SceneChange(sceneName);
        isPushButton = true;
    }
}
