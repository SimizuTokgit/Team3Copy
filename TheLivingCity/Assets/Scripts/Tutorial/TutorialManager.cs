using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//清水 チュートリアルを今のゲームに合わせて作り直した
//ステップのはじめにゲームを止めて、やり方の画像と説明を大きく出す（説明画面）
//説明画面は3秒たつまでボタンを受け付けない（走っているときに押していたボタンで飛ばさないように）。そのあと何かボタンで再開
//やってみるステップは、画面上の小さなパネルに目標と進み具合を出す。読むだけのステップは、説明画面を閉じたら次へ
public class TutorialManager : MonoBehaviour
{
    public enum TutorialState
    {
        Challenge,
        ChallengeDodge,
        Clear,
        Next,
        Finish,
        Read,      //説明画面（ゲームは止まっている）
        WaitStart, //スタートのカウントダウンが終わるのを待つ
    };
    public TutorialState tutorialstate;

    //ステップの種類。ほかのスクリプトは番号ではなく種類で「今どのステップか」を調べる
    public enum StepKind
    {
        Move,     //左右移動
        Landing,  //ジャスト着地
        Items,    //アイテムを集める
        Dodge,    //障害物をよけ続ける（今のチュートリアルは障害物を出さないので使っていない）
        Read,     //説明を読むだけ
    }

    [System.Serializable]
    public class Step
    {
        public StepKind Kind;
        public string Title;
        [TextArea(2, 4)] public string Body;
        public string Goal;   //目標。{0} に進み具合が入る（読むだけのステップは空でよい）
        public string Keys;   //使うキー（空なら出さない）
        public Texture2D Picture;   //やり方の画像（空なら Resources/Tutorial/PictureName を読む）
        public string PictureName;
    }

    [SerializeField] private List<Step> Steps = DefaultSteps();

    //チュートリアルのテキスト（シーンにある古い表示。今はパネルと説明画面を使うので隠す）
    public Text tutorialText;

    //チュートリアル何番目か
    public int tutorialCount = 0;

    //チュートリアルクリアしたかどうか
    public bool tutorialClear;

    //次のチュートリアルに移るための時間
    [SerializeField]
    private float nexttutorialTime;
    private float elapsedTime;

    //障害物をよける　何秒避けるか
    [SerializeField]
    private float DodgeTime;
    //避けている時間
    private float dodgeTime;

    //アイテムをいくつ集めたらクリアか（PlayerMagnetManager と同じ数）
    private const int ItemGoal = 5;

    //説明画面は、この秒数たつまでボタンを受け付けない
    [SerializeField] private float ExplainWaitTime = 3f;

    //チュートリアルすべて完了したかどうか
    public bool tutorialClearAll;

    //すべて完了したときに出すテキスト（今は説明画面に出すので隠す）
    public Text FinishText;

    //上の小さなパネル（やってみるステップの目標）
    private GameObject _panelObj;
    private Text _stepLabel, _title, _goal, _keys;

    //説明画面
    private GameObject _modal;
    private Text _mStep, _mTitle, _mBody, _mPrompt;
    private RawImage _mPicture;
    private Image _mWaitBar;
    private float _explainTime;
    private bool _explainOpen;
    private bool _finishing;

    private GameManager _gameManager;

    private static readonly Color Cyan = new Color(0.47f, 0.88f, 1f);
    private static readonly Color GoalColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ClearColor = new Color(0.45f, 1f, 0.55f);

    public Step Current => tutorialCount < Steps.Count ? Steps[tutorialCount] : null;

    /// <summary>
    /// 今そのステップをやっているか（説明画面の間と、クリアしたあとの待ち時間は false）
    /// </summary>
    public bool IsStep(StepKind kind)
    {
        return !tutorialClearAll && !tutorialClear && !_explainOpen && Current != null && Current.Kind == kind;
    }

    /// <summary>
    /// アイテムを出してよいか（左右移動とジャスト着地のあいだは、走ることに集中できるように出さない）
    /// </summary>
    public bool SpawnObstacles
    {
        get
        {
            for (int i = 0; i <= tutorialCount && i < Steps.Count; i++)
            {
                if (Steps[i].Kind == StepKind.Items || Steps[i].Kind == StepKind.Dodge) return true;
            }
            return tutorialClearAll;
        }
    }

    void Start()
    {
        tutorialCount = 0;
        tutorialClear = false;
        tutorialClearAll = false;
        elapsedTime = 0;
        dodgeTime = 0.0f;
        if (nexttutorialTime <= 0f) nexttutorialTime = 2f;
        if (DodgeTime <= 0f) DodgeTime = 5f;

        _gameManager = FindAnyObjectByType<GameManager>();

        BuildPanel();
        BuildModal();
        if (tutorialText) tutorialText.enabled = false;
        if (FinishText) FinishText.enabled = false;

        //スタートの 3, 2, 1, GO が終わってから最初の説明を出す
        tutorialstate = TutorialState.WaitStart;
    }

    private void OnDestroy()
    {
        //止めたまま別のシーンに行かないように
        Time.timeScale = 1f;
    }

    void Update()
    {
        //清水 ポーズ中（Esc）は止めておく（よける秒数が進んだり、次の説明が出たりしないように）
        if (_gameManager && _gameManager._currentState == GameManager.GameState.Pause) return;

        if (tutorialstate == TutorialState.WaitStart)
        {
            if (!_gameManager || _gameManager._currentState == GameManager.GameState.Playing) BeginStep();
        }
        else if (tutorialstate == TutorialState.Read || tutorialstate == TutorialState.Finish)
        {
            UpdateExplain();
        }
        else if (tutorialstate == TutorialState.Challenge)
        {
            if (tutorialClear)
                tutorialstate = TutorialState.Clear;
        }
        else if (tutorialstate == TutorialState.ChallengeDodge)
        {
            dodgeTime += Time.deltaTime;
            ShowGoal(string.Format(Current.Goal, Mathf.FloorToInt(dodgeTime) + " / " + DodgeTime.ToString("F0") + "秒"), GoalColor);

            if (DodgeTime <= dodgeTime)
            {
                ClearTutorial();
            }

            if (tutorialClear)
                tutorialstate = TutorialState.Clear;
        }
        else if (tutorialstate == TutorialState.Clear)
        {
            NextTutorial();
        }
        else if (tutorialstate == TutorialState.Next)
        {
            //次のチュートリアルまで何秒か待つ
            elapsedTime += Time.deltaTime;

            if (nexttutorialTime <= elapsedTime)
            {
                BeginStep();
            }
        }
    }

    //ステップのはじめ：ゲームを止めて説明画面を出す
    private void BeginStep()
    {
        tutorialClear = false;
        elapsedTime = 0f;
        dodgeTime = 0f;

        Step step = Current;
        OpenExplain("STEP " + (tutorialCount + 1) + " / " + Steps.Count, step.Title, step.Body, LoadPicture(step),
                    step.Kind == StepKind.Read ? "何かボタンを押して次へ" : "何かボタンを押して、やってみよう！");
        tutorialstate = TutorialState.Read;
    }

    //説明画面を閉じたあと：やってみるステップなら目標のパネルを出して再開
    private void StartChallenge()
    {
        Step step = Current;
        if (step.Kind == StepKind.Read)
        {
            ClearTutorial();
            tutorialstate = TutorialState.Clear;
            return;
        }

        _panelObj.SetActive(true);
        _stepLabel.text = "STEP " + (tutorialCount + 1) + " / " + Steps.Count;
        _title.text = step.Title;
        _keys.text = string.IsNullOrEmpty(step.Keys) ? "" : "操作：" + step.Keys;

        switch (step.Kind)
        {
            case StepKind.Dodge:
                tutorialstate = TutorialState.ChallengeDodge;
                break;
            case StepKind.Items:
                ItemGetTextNumberChange(0);
                tutorialstate = TutorialState.Challenge;
                break;
            default:
                ShowGoal(string.Format(step.Goal ?? "", ""), GoalColor);
                tutorialstate = TutorialState.Challenge;
                break;
        }
    }

    //次のチュートリアルに移動
    public void NextTutorial()
    {
        tutorialCount++;

        if (Steps.Count <= tutorialCount)
        {
            tutorialClearAll = true;
            Debug.Log("チュートリアル完了");
            _panelObj.SetActive(false);
            GameHUD.Get().Banner("ALL CLEAR!", GameHUD.Gold, 2.0f);
            OpenExplain("COMPLETE", "チュートリアル完了！", "これで準備はばっちり。\nさあ、崩壊した街を、希望を捨てずに走り抜けよう！", null, "何かボタンを押してタイトルへ");
            tutorialstate = TutorialState.Finish;
            return;
        }

        //読むだけのステップのあとは、CLEAR は出さずにすぐ次の説明へ
        bool wasRead = Steps[tutorialCount - 1].Kind == StepKind.Read;
        if (!wasRead)
        {
            ShowGoal("クリア！", ClearColor);
            GameHUD.Get().Banner("CLEAR!", GameHUD.Good, 1.2f);
        }
        else
        {
            _panelObj.SetActive(false);
        }

        elapsedTime = wasRead ? nexttutorialTime : 0.0f;
        tutorialstate = TutorialState.Next;
    }

    //アイテムをゲットするチュートリアルアイテムゲット数
    public void ItemGetTextNumberChange(int number)
    {
        if (Current == null) return;
        ShowGoal(string.Format(Current.Goal, number + " / " + ItemGoal), GoalColor);
    }

    //障害物を避けるチュートリアルで障害物に当たった時
    public void DodgeTimeReset()
    {
        dodgeTime = 0.0f;
    }

    //クリアした時に呼ぶ
    public void ClearTutorial()
    {
        tutorialClear = true;
    }

    private void ShowGoal(string text, Color color)
    {
        _goal.text = text;
        _goal.color = color;
    }

    // ===== 説明画面 =====

    private void OpenExplain(string step, string title, string body, Texture2D picture, string prompt)
    {
        _mStep.text = step;
        _mTitle.text = title;
        _mBody.text = body;
        _mPrompt.text = prompt;
        _mPicture.texture = picture;
        _mPicture.transform.parent.gameObject.SetActive(picture != null);

        _modal.SetActive(true);
        _explainOpen = true;
        _explainTime = 0f;
        Time.timeScale = 0f; //ゲームを止める
    }

    private void UpdateExplain()
    {
        _explainTime += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_explainTime / Mathf.Max(0.01f, ExplainWaitTime));
        bool ready = k >= 1f;

        //3秒たつまでは残り時間のバーを出して、ボタンを受け付けない
        _mWaitBar.rectTransform.anchorMax = new Vector2(1f - k, 1f); //残り時間に合わせて短くなる
        _mWaitBar.transform.parent.gameObject.SetActive(!ready);
        Color c = _mPrompt.color;
        c.a = ready ? Mathf.Lerp(0.45f, 1f, Mathf.PingPong(Time.unscaledTime * 2f, 1f)) : 0.25f;
        _mPrompt.color = c;

        if (!ready || !AnyButtonPressed()) return;

        if (tutorialstate == TutorialState.Finish)
        {
            if (_finishing) return;
            _finishing = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
            return;
        }

        //閉じて再開
        _modal.SetActive(false);
        _explainOpen = false;
        Time.timeScale = 1f;
        StartChallenge();
    }

    //何かのボタン（キーボードのどのキーでも、ゲームパッドのどのボタンでも、マウスのクリックでも）
    private static bool AnyButtonPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
        Gamepad g = Gamepad.current;
        if (g != null)
        {
            foreach (InputControl control in g.allControls)
            {
                if (control is ButtonControl b && !b.synthetic && b.wasPressedThisFrame) return true;
            }
        }
        return false;
    }

    private Texture2D LoadPicture(Step step)
    {
        if (step == null) return null;
        if (step.Picture) return step.Picture;
        return string.IsNullOrEmpty(step.PictureName) ? null : Resources.Load<Texture2D>("Tutorial/" + step.PictureName);
    }

    // ===== 画面の部品 =====

    private Canvas GetCanvas()
    {
        return tutorialText ? tutorialText.canvas.rootCanvas : FindAnyObjectByType<Canvas>();
    }

    private Font GetFont()
    {
        return tutorialText ? tutorialText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    //上の小さなパネル
    private void BuildPanel()
    {
        Font font = GetFont();
        _panelObj = NewImage("TutorialPanel", GetCanvas().transform, new Color(0.02f, 0.04f, 0.12f, 0.82f));
        RectTransform pr = (RectTransform)_panelObj.transform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
        pr.pivot = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, -24f);
        pr.sizeDelta = new Vector2(1100f, 190f);
        AccentBar(_panelObj.transform);

        _stepLabel = MakeText(_panelObj.transform, font, 24, Cyan, FontStyle.Bold, TextAnchor.UpperLeft, -14f, 30f);
        _title = MakeText(_panelObj.transform, font, 44, Color.white, FontStyle.Bold, TextAnchor.UpperLeft, -42f, 56f);
        _goal = MakeText(_panelObj.transform, font, 34, GoalColor, FontStyle.Bold, TextAnchor.UpperLeft, -100f, 44f);
        _keys = MakeText(_panelObj.transform, font, 24, Cyan, FontStyle.Normal, TextAnchor.UpperLeft, -148f, 32f);
        _panelObj.SetActive(false);
    }

    //説明画面（全体を暗くして、まん中に大きなカード）
    private void BuildModal()
    {
        Font font = GetFont();
        _modal = NewImage("TutorialExplain", GetCanvas().transform, new Color(0f, 0f, 0f, 0.6f));
        RectTransform mr = (RectTransform)_modal.transform;
        mr.anchorMin = Vector2.zero;
        mr.anchorMax = Vector2.one;
        mr.offsetMin = mr.offsetMax = Vector2.zero;

        GameObject card = NewImage("Card", _modal.transform, new Color(0.02f, 0.04f, 0.12f, 0.95f));
        RectTransform cr = (RectTransform)card.transform;
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(1500f, 1000f);
        AccentBar(card.transform);

        _mStep = MakeText(card.transform, font, 28, Cyan, FontStyle.Bold, TextAnchor.UpperCenter, -24f, 36f);
        _mTitle = MakeText(card.transform, font, 64, Color.white, FontStyle.Bold, TextAnchor.UpperCenter, -62f, 80f);

        //画像（水色のふち付き）
        GameObject frame = NewImage("PictureFrame", card.transform, Cyan);
        RectTransform fr = (RectTransform)frame.transform;
        fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 1f);
        fr.pivot = new Vector2(0.5f, 1f);
        fr.anchoredPosition = new Vector2(0f, -150f);
        fr.sizeDelta = new Vector2(968f, 552f);
        GameObject pic = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
        pic.transform.SetParent(frame.transform, false);
        RectTransform pir = (RectTransform)pic.transform;
        pir.anchorMin = Vector2.zero;
        pir.anchorMax = Vector2.one;
        pir.offsetMin = new Vector2(4f, 4f);
        pir.offsetMax = new Vector2(-4f, -4f);
        _mPicture = pic.GetComponent<RawImage>();
        _mPicture.raycastTarget = false;

        _mBody = MakeText(card.transform, font, 32, new Color(0.88f, 0.92f, 1f), FontStyle.Normal, TextAnchor.UpperCenter, -716f, 150f);
        _mPrompt = MakeText(card.transform, font, 40, GoalColor, FontStyle.Bold, TextAnchor.UpperCenter, -880f, 52f);

        //ボタンを受け付けるまでの残り時間のバー
        GameObject barBg = NewImage("WaitBar", card.transform, new Color(1f, 1f, 1f, 0.15f));
        RectTransform bb = (RectTransform)barBg.transform;
        bb.anchorMin = bb.anchorMax = new Vector2(0.5f, 1f);
        bb.pivot = new Vector2(0.5f, 1f);
        bb.anchoredPosition = new Vector2(0f, -946f);
        bb.sizeDelta = new Vector2(600f, 12f);
        GameObject bar = NewImage("Fill", barBg.transform, GoalColor);
        RectTransform br = (RectTransform)bar.transform;
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = br.offsetMax = Vector2.zero;
        _mWaitBar = bar.GetComponent<Image>();

        _modal.SetActive(false);
    }

    private static GameObject NewImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image img = obj.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return obj;
    }

    private static void AccentBar(Transform parent)
    {
        GameObject bar = NewImage("Accent", parent, Cyan);
        RectTransform br = (RectTransform)bar.transform;
        br.anchorMin = new Vector2(0f, 0f);
        br.anchorMax = new Vector2(0f, 1f);
        br.pivot = new Vector2(0f, 0.5f);
        br.sizeDelta = new Vector2(10f, 0f);
    }

    private static Text MakeText(Transform parent, Font font, int size, Color color, FontStyle style, TextAnchor align, float y, float height)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
        obj.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)obj.transform;
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, y);
        r.sizeDelta = new Vector2(-96f, height);

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.1f;
        t.raycastTarget = false;
        t.supportRichText = true;

        Shadow s = obj.GetComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.8f);
        s.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    //ステップの中身（インスペクターで書きかえられる）
    private static List<Step> DefaultSteps()
    {
        return new List<Step>
        {
            new Step
            {
                Kind = StepKind.Move,
                PictureName = "step1_move",
                Title = "左右に曲がろう",
                Body = "走るのは自動。プレイヤーは左右に曲がって、進む向きを決めるよ。\nA / D キー（← / → キー、左スティック）で曲がろう。",
                Goal = "目標：左と右、両方に曲がってみよう",
                Keys = "A / D キー（← / → キー）、左スティック",
            },
            new Step
            {
                Kind = StepKind.Landing,
                PictureName = "step2_landing",
                Title = "ジャスト着地でブースト！",
                Body = "丘を越えると空を飛ぶ。W キー（ゲームパッドは Aボタン）で急降下して、\n<b>下り坂に向かって</b>着地すると PERFECT！ スピードが一気に上がるよ。",
                Goal = "目標：PERFECT 着地を1回しよう",
                Keys = "W キー（ゲームパッドは Aボタン）で急降下",
            },
            new Step
            {
                Kind = StepKind.Items,
                PictureName = "step3_items",
                Title = "アイテムで強化しよう",
                Body = "近づくだけで吸い寄せられて取れる。取るほど強くなって、上限はなし！\n最高速度・加速・ブースト・マグネット・小回り・防御の6種類があるよ。",
                Goal = "目標：アイテムを集めよう　{0}",
                Keys = "",
            },
            new Step
            {
                Kind = StepKind.Read,
                PictureName = "step4_obstacles",
                Title = "障害物に気をつけよう",
                Body = "本番の道には障害物がある。当たると HP が減って、スピードも落ちる。HP が 0 でゲームオーバー。\n木や看板など細いものは、当たっても減りが少ないよ。ブースト中は当たっても平気！",
                Goal = "",
                Keys = "",
            },
            new Step
            {
                Kind = StepKind.Read,
                PictureName = "step5_checkpoint",
                Title = "チェックポイントと分かれ道",
                Body = "チェックポイントを通ると HP が回復する（エンドレスでは残り時間も）。\nその先の分かれ道では、左右の看板のアイテムを見て道を選ぼう。道によって難しさともらえるアイテムが変わるよ。",
                Goal = "",
                Keys = "",
            },
            new Step
            {
                Kind = StepKind.Read,
                PictureName = "step6_boss",
                Title = "ボスと一撃必殺のギミック",
                Body = "ストーリーでは4体の中ボスと、最後にマザーコンピューターが現れる。最高速で突っ込もう、<b>速いほど大ダメージ</b>！\n震える・赤く光る・地面に赤い帯が出るものは一撃必殺の合図。よく見てかわそう。",
                Goal = "",
                Keys = "",
            },
        };
    }
}
