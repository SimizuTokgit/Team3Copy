using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//清水 チュートリアルを今のゲームに合わせて作り直した
//画面上のパネルに「STEP 番号・タイトル・説明・目標（進み具合）・使うキー」を出す
//やってみるステップ（左右移動・ジャスト着地・アイテム・障害物）と、読むだけのステップ（チェックポイント・ボス）がある
public class TutorialManager : MonoBehaviour
{
    public enum TutorialState
    {
        Challenge,
        ChallengeDodge,
        Clear,
        Next,
        Finish,
        Read,      //読むだけのステップ（Enter / Aボタンで次へ）
    };
    public TutorialState tutorialstate;

    //ステップの種類。ほかのスクリプトは番号ではなく種類で「今どのステップか」を調べる
    public enum StepKind
    {
        Move,     //左右移動
        Landing,  //ジャスト着地
        Items,    //アイテムを集める
        Dodge,    //障害物をよけ続ける
        Read,     //説明を読む
    }

    [System.Serializable]
    public class Step
    {
        public StepKind Kind;
        public string Title;
        [TextArea(2, 4)] public string Body;
        public string Goal;   //目標。{0} に進み具合が入る
        public string Keys;   //使うキー（空なら出さない）
        public Texture2D Picture;   //やり方の画像（空なら Resources/Tutorial/PictureName を読む）
        public string PictureName;
    }

    [SerializeField] private List<Step> Steps = DefaultSteps();

    //チュートリアルのテキスト（シーンにある古い1行の表示。今は下のパネルを使うので隠す）
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

    //読むだけのステップは、これより早くは進めない（読み飛ばし防止）／これだけたったら自動で進む
    [SerializeField] private float ReadMinTime = 1.0f;
    [SerializeField] private float ReadAutoTime = 15f;

    //チュートリアルすべて完了したかどうか
    public bool tutorialClearAll;

    //すべて完了したときに出すテキスト（今はパネルに出すので隠す）
    public Text FinishText;

    //パネル
    private Text _stepLabel, _title, _body, _goal, _keys;
    private RectTransform _panel;
    private RawImage _picture;
    private const float PanelWidth = 1240f;
    private const float PictureWidth = 480f;   //16:9 の画像を右に出す
    private const float PictureHeight = 270f;
    private static readonly Color Cyan = new Color(0.47f, 0.88f, 1f);
    private static readonly Color GoalColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ClearColor = new Color(0.45f, 1f, 0.55f);

    public Step Current => tutorialCount < Steps.Count ? Steps[tutorialCount] : null;

    /// <summary>
    /// 今そのステップをやっているか（クリアしたあとの待ち時間は false）
    /// </summary>
    public bool IsStep(StepKind kind)
    {
        return !tutorialClearAll && !tutorialClear && Current != null && Current.Kind == kind;
    }

    /// <summary>
    /// 障害物とアイテムを出してよいか（左右移動とジャスト着地のあいだは、走ることに集中できるように出さない）
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

        BuildPanel();
        if (tutorialText) tutorialText.enabled = false;
        if (FinishText) FinishText.enabled = false;

        BeginStep();
    }

    void Update()
    {
        if (tutorialstate == TutorialState.Challenge)
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
        else if (tutorialstate == TutorialState.Read)
        {
            elapsedTime += Time.deltaTime;
            if ((elapsedTime >= ReadMinTime && ConfirmPressed()) || elapsedTime >= ReadAutoTime)
            {
                ClearTutorial();
                tutorialstate = TutorialState.Clear;
            }
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
        else if (tutorialstate == TutorialState.Finish)
        {
            //全てのチュートリアルを完了したときの処理
            if (ConfirmPressed())
            {
                //タイトルに戻る
                SceneManager.LoadScene("Title");
            }
        }
    }

    //今のステップを始める
    private void BeginStep()
    {
        tutorialClear = false;
        elapsedTime = 0f;
        dodgeTime = 0f;

        Step step = Current;
        _stepLabel.text = "STEP " + (tutorialCount + 1) + " / " + Steps.Count;
        _title.text = step.Title;
        _body.text = step.Body;
        _keys.text = string.IsNullOrEmpty(step.Keys) ? "" : "操作：" + step.Keys;
        ShowPicture(step);

        switch (step.Kind)
        {
            case StepKind.Dodge:
                tutorialstate = TutorialState.ChallengeDodge;
                break;
            case StepKind.Read:
                ShowGoal(step.Goal, GoalColor);
                tutorialstate = TutorialState.Read;
                break;
            case StepKind.Items:
                ItemGetTextNumberChange(0);
                tutorialstate = TutorialState.Challenge;
                break;
            default:
                ShowGoal(string.Format(step.Goal, ""), GoalColor);
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

            _stepLabel.text = "COMPLETE";
            _title.text = "チュートリアル完了！";
            _body.text = "これで準備はばっちり。\nさあ、崩壊した街を、希望を捨てずに走り抜けよう！";
            _keys.text = "";
            ShowPicture(null);
            ShowGoal("Enterキー / Aボタンでタイトルへ", ClearColor);
            GameHUD.Get().Banner("ALL CLEAR!", GameHUD.Gold, 2.0f);

            tutorialstate = TutorialState.Finish;
            return;
        }

        //読むだけのステップのあとは、CLEAR は出さずにすぐ次へ
        bool wasRead = Steps[tutorialCount - 1].Kind == StepKind.Read;
        if (!wasRead)
        {
            ShowGoal("クリア！", ClearColor);
            GameHUD.Get().Banner("CLEAR!", GameHUD.Good, 1.2f);
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

    //Enter・スペース・ゲームパッドのAボタン
    private static bool ConfirmPressed()
    {
        Keyboard k = Keyboard.current;
        if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) return true;
        Gamepad g = Gamepad.current;
        return g != null && g.buttonSouth.wasPressedThisFrame;
    }

    // ===== パネル =====

    private void BuildPanel()
    {
        Canvas canvas = tutorialText ? tutorialText.canvas.rootCanvas : FindAnyObjectByType<Canvas>();
        Font font = tutorialText ? tutorialText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform pr = (RectTransform)panel.transform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
        pr.pivot = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, -24f);
        pr.sizeDelta = new Vector2(PanelWidth, 330f);
        _panel = pr;
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.12f, 0.82f);
        bg.raycastTarget = false;

        //左のアクセントの線
        GameObject bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(panel.transform, false);
        RectTransform br = (RectTransform)bar.transform;
        br.anchorMin = new Vector2(0f, 0f);
        br.anchorMax = new Vector2(0f, 1f);
        br.pivot = new Vector2(0f, 0.5f);
        br.sizeDelta = new Vector2(10f, 0f);
        bar.GetComponent<Image>().color = Cyan;
        bar.GetComponent<Image>().raycastTarget = false;

        _stepLabel = MakeText(panel.transform, font, 26, Cyan, FontStyle.Bold, -16f, 34f);
        _title = MakeText(panel.transform, font, 50, Color.white, FontStyle.Bold, -48f, 64f);
        _body = MakeText(panel.transform, font, 28, new Color(0.88f, 0.92f, 1f), FontStyle.Normal, -118f, 120f);
        _goal = MakeText(panel.transform, font, 34, GoalColor, FontStyle.Bold, -240f, 44f);
        _keys = MakeText(panel.transform, font, 24, Cyan, FontStyle.Normal, -286f, 34f);

        //右側のやり方の画像（水色のふち付き）
        GameObject frame = new GameObject("PictureFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        RectTransform fr = (RectTransform)frame.transform;
        fr.anchorMin = fr.anchorMax = new Vector2(1f, 0.5f);
        fr.pivot = new Vector2(1f, 0.5f);
        fr.anchoredPosition = new Vector2(-24f, 0f);
        fr.sizeDelta = new Vector2(PictureWidth + 8f, PictureHeight + 8f);
        frame.GetComponent<Image>().color = Cyan;
        frame.GetComponent<Image>().raycastTarget = false;

        GameObject pic = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
        pic.transform.SetParent(frame.transform, false);
        RectTransform pir = (RectTransform)pic.transform;
        pir.anchorMin = Vector2.zero;
        pir.anchorMax = Vector2.one;
        pir.offsetMin = new Vector2(4f, 4f);
        pir.offsetMax = new Vector2(-4f, -4f);
        _picture = pic.GetComponent<RawImage>();
        _picture.raycastTarget = false;
        ShowPicture(null);
    }

    //画像があればパネルを右に広げて出す。無ければ文字だけ
    private void ShowPicture(Step step)
    {
        Texture2D tex = null;
        if (step != null)
        {
            tex = step.Picture;
            if (!tex && !string.IsNullOrEmpty(step.PictureName)) tex = Resources.Load<Texture2D>("Tutorial/" + step.PictureName);
        }

        bool show = tex != null;
        _picture.texture = tex;
        _picture.transform.parent.gameObject.SetActive(show);

        float extra = show ? PictureWidth + 40f : 0f;
        _panel.sizeDelta = new Vector2(PanelWidth + extra, _panel.sizeDelta.y);
        foreach (Text t in new[] { _stepLabel, _title, _body, _goal, _keys })
        {
            RectTransform r = t.rectTransform;
            r.offsetMax = new Vector2(-56f - extra, r.offsetMax.y);
        }
    }

    private static Text MakeText(Transform parent, Font font, int size, Color color, FontStyle style, float y, float height)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
        obj.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)obj.transform;
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(16f, y);
        r.sizeDelta = new Vector2(-72f, height);

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.UpperLeft;
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
                Body = "走るのは自動。プレイヤーは左右に曲がって進む向きを決めるよ。\n障害物や危ないものは、曲がってよけよう。",
                Goal = "目標：左と右、両方に曲がってみよう",
                Keys = "A / D キー（← / → キー）、左スティック",
            },
            new Step
            {
                Kind = StepKind.Landing,
                PictureName = "step2_landing",
                Title = "ジャスト着地でブースト！",
                Body = "丘を越えると空を飛ぶ。W キーで急降下して、<b>下り坂に向かって</b>着地すると PERFECT！\nスピードが一気に上がって、ブースト中は障害物に当たってもへっちゃら。",
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
                Kind = StepKind.Dodge,
                PictureName = "step4_obstacles",
                Title = "障害物をよけよう",
                Body = "障害物に当たると HP が減って、スピードも落ちる。HP が 0 になるとゲームオーバー。\n木や看板など細いものは、当たっても減りが少ないよ。",
                Goal = "目標：当たらずに走り続けよう　{0}",
                Keys = "当たると、数えなおし",
            },
            new Step
            {
                Kind = StepKind.Read,
                Title = "チェックポイントと分かれ道",
                PictureName = "step5_checkpoint",
                Body = "チェックポイントを通ると HP が回復する（エンドレスでは残り時間も）。\nその先の分かれ道では、左右の看板のアイテムを見て道を選ぼう。道によって難しさともらえるアイテムが変わるよ。",
                Goal = "Enterキー / Aボタンで次へ",
                Keys = "",
            },
            new Step
            {
                Kind = StepKind.Read,
                Title = "ボスと一撃必殺のギミック",
                PictureName = "step6_boss",
                Body = "ストーリーでは4体の中ボスと、最後にマザーコンピューターが現れる。ジャンプ台から最高速で突っ込もう、<b>速いほど大ダメージ</b>！\n震える・赤く光る・地面に赤い帯が出るものは一撃必殺の合図。よく見てかわそう。",
                Goal = "Enterキー / Aボタンで次へ",
                Keys = "",
            },
        };
    }
}
