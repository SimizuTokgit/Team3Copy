using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//清水
//ポーズ画面（Esc で開く設定画面）に「Back（ゲームに戻る）」と「Quit（タイトルに戻る）」を出す
//GameManager の PauseImage の下にスクリプトでボタンを作るので、シーンをいじらなくてよい
//画像と音は Resources/UI/Pause に入れてある（タイトルの Back / Quit と同じ絵）
//W / S・↑↓・左スティック上下・十字キー上下で選んで、Enter / Space / パッドの A（またはクリック）で決定
//（左右は音量バーが使うので、ボタンの移動には使わない）
public class PauseMenu : MonoBehaviour
{
    private const string ResourceDir = "UI/Pause/";

    //見た目の位置と大きさ（Pause の画面 1920x1080 の中心から）
    private const float ButtonScale = 0.8f;
    private static readonly Vector2 BackPos = new Vector2(110f, -200f); //Back の画像は左に線があるので少し右に寄せると文字が真ん中に来る
    private static readonly Vector2 QuitPos = new Vector2(0f, -360f);

    /// <summary>タイトルに戻る途中か（このあいだは Esc でゲームに戻らない）</summary>
    public static bool IsQuitting { get; private set; }

    private Button backButton;
    private Button quitButton;

    /// <summary>
    /// PauseImage の下に Back / Quit を作る。GameManager の Awake から呼ぶ
    /// </summary>
    public static PauseMenu Create(GameObject pauseImage)
    {
        IsQuitting = false;
        if (!pauseImage) return null;

        //二重に作らない
        PauseMenu existing = pauseImage.GetComponentInChildren<PauseMenu>(true);
        if (existing) return existing;

        GameObject root = new GameObject("PauseMenu", typeof(RectTransform));
        root.layer = pauseImage.layer;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(pauseImage.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        PauseMenu menu = root.AddComponent<PauseMenu>();

        AudioClip selectSE = Resources.Load<AudioClip>(ResourceDir + "Pause_Select");
        AudioClip pushSE = Resources.Load<AudioClip>(ResourceDir + "Pause_Push");

        menu.backButton = MakeButton(root.transform, "BackButton", "Pause_BackOFF", "Pause_BackON", "BACK", BackPos, selectSE, pushSE, menu.Back);
        menu.quitButton = MakeButton(root.transform, "QuitButton", "Pause_QuitOFF", "Pause_QuitON", "QUIT", QuitPos, selectSE, pushSE, menu.Quit);

        //開いたら Back を選ぶ。上下で移動、端まで行ったら反対へ
        MenuNavigation.Attach(root, MenuNavigation.Direction.Vertical, false, menu.backButton,
            new Selectable[] { menu.backButton, menu.quitButton });

        return menu;
    }

    private static Button MakeButton(Transform parent, string name, string offName, string onName, string fallbackText,
        Vector2 position, AudioClip selectSE, AudioClip pushSE, UnityAction onClick)
    {
        Sprite off = Resources.Load<Sprite>(ResourceDir + offName);
        Sprite on = Resources.Load<Sprite>(ResourceDir + onName);

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = off ? off.rect.size * ButtonScale : new Vector2(360f, 110f);

        Image image = go.GetComponent<Image>();
        image.sprite = off;

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None; //見た目は ButtonSpriteChanger が ON / OFF 画像で変える
        button.onClick.AddListener(onClick);

        if (off)
        {
            ButtonSpriteChanger changer = go.AddComponent<ButtonSpriteChanger>();
            changer.Setup(off, on, selectSE, pushSE);
        }
        else
        {
            //画像が見つからないときでも押せるように、文字だけのボタンにする
            image.color = new Color(0f, 0.05f, 0.15f, 0.8f);
            button.transition = Selectable.Transition.ColorTint;

            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            label.layer = go.layer;
            RectTransform lr = label.GetComponent<RectTransform>();
            lr.SetParent(go.transform, false);
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;

            Text text = label.GetComponent<Text>();
            text.text = fallbackText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        return button;
    }

    /// <summary>
    /// Back：ゲームに戻る（Esc をもう一度押したのと同じ）
    /// </summary>
    public void Back()
    {
        if (IsQuitting) return;

        PlayerController controller = FindAnyObjectByType<PlayerController>();
        if (controller) controller.Resume();
    }

    /// <summary>
    /// Quit：タイトルに戻る
    /// </summary>
    public void Quit()
    {
        if (IsQuitting) return;
        IsQuitting = true;

        if (backButton) backButton.interactable = false;
        if (quitButton) quitButton.interactable = false;

        Time.timeScale = 1f; //止まったままだとフェードが進まない

        FadeImageController fade = FindFirstObjectByType<FadeImageController>();
        if (fade) fade.SceneChange("Title");
        else SceneManager.LoadScene("Title");
    }

    private void OnDestroy()
    {
        //次のシーンに持ち越さない
        IsQuitting = false;
    }
}
