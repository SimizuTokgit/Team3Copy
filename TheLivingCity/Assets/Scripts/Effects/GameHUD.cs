using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//清水
//プレイ中に出す追加のUI（何が起きたかを文字で知らせる）
//・画面中央のバナー（CHECKPOINT / WARNING / HIT など）
//・アイテムを取ったときのポップアップ
//・右上の情報（次のボスまでの距離、エンドレスの走行距離とベスト）
//・ボスのHPバー
//・電車が来る方向の警告
//・画面フラッシュ
//シーンに置かなくても GameHUD.Get() で勝手にできる
//日本語フォントが TMP に無いので表示は英語
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    public static GameHUD Get()
    {
        if (!Instance) new GameObject("GameHUD").AddComponent<GameHUD>();
        return Instance;
    }

    //色
    public static readonly Color Good = new Color(0.4f, 1f, 0.5f);
    public static readonly Color Danger = new Color(1f, 0.25f, 0.2f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

    private RectTransform _root;
    private TextMeshProUGUI _banner;
    private TextMeshProUGUI _info;
    private TextMeshProUGUI _warnLeft;
    private TextMeshProUGUI _warnRight;
    private Image _flash;

    private GameObject _bossBar;
    private TextMeshProUGUI _bossName;
    private RectTransform _bossFill;
    private float _bossFillTarget = 1f;

    private Coroutine _bannerCoroutine;
    private Coroutine _flashCoroutine;
    private float _warnLeftTime;
    private float _warnRightTime;

    private Transform _player;
    private GameManager _gameManager;
    private BossBattleManager _bossBattle;
    private float _startZ;
    private bool _isStarted = false;
    private int _popupCount = 0;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildCanvas();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Player player = FindAnyObjectByType<Player>();
        if (player) _player = player.transform;
        _gameManager = FindAnyObjectByType<GameManager>();
        _bossBattle = FindAnyObjectByType<BossBattleManager>();
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _root = (RectTransform)transform;

        //フラッシュ（いちばん後ろ）
        _flash = CreateImage("Flash", _root, new Color(1, 1, 1, 0));
        Stretch(_flash.rectTransform);
        _flash.raycastTarget = false;

        //中央バナー
        _banner = CreateText("Banner", _root, 110, TextAlignmentOptions.Center);
        SetRect(_banner.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1600, 300));
        _banner.gameObject.SetActive(false);

        //右上の情報
        _info = CreateText("Info", _root, 40, TextAlignmentOptions.TopRight);
        SetRect(_info.rectTransform, new Vector2(1f, 1f), new Vector2(700, 200), new Vector2(1f, 1f), new Vector2(-40, -150));

        //電車の警告
        _warnLeft = CreateText("WarnLeft", _root, 70, TextAlignmentOptions.Left);
        SetRect(_warnLeft.rectTransform, new Vector2(0f, 0.5f), new Vector2(600, 150), new Vector2(0f, 0.5f), new Vector2(40, 0));
        _warnLeft.color = Danger;
        _warnLeft.text = "<< TRAIN!";
        _warnLeft.gameObject.SetActive(false);

        _warnRight = CreateText("WarnRight", _root, 70, TextAlignmentOptions.Right);
        SetRect(_warnRight.rectTransform, new Vector2(1f, 0.5f), new Vector2(600, 150), new Vector2(1f, 0.5f), new Vector2(-40, 0));
        _warnRight.color = Danger;
        _warnRight.text = "TRAIN! >>";
        _warnRight.gameObject.SetActive(false);

        //ボスHPバー
        _bossBar = new GameObject("BossBar", typeof(RectTransform));
        RectTransform barRect = (RectTransform)_bossBar.transform;
        barRect.SetParent(_root, false);
        SetRect(barRect, new Vector2(0.5f, 1f), new Vector2(900, 90), new Vector2(0.5f, 1f), new Vector2(0, -40));

        _bossName = CreateText("BossName", barRect, 38, TextAlignmentOptions.Bottom);
        SetRect(_bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 50), new Vector2(0.5f, 1f), Vector2.zero);

        Image back = CreateImage("Back", barRect, new Color(0, 0, 0, 0.6f));
        SetRect(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(900, 30), new Vector2(0.5f, 0f), Vector2.zero);

        Image fill = CreateImage("Fill", back.rectTransform, Danger);
        _bossFill = fill.rectTransform;
        _bossFill.anchorMin = new Vector2(0, 0);
        _bossFill.anchorMax = new Vector2(1, 1);
        _bossFill.offsetMin = new Vector2(4, 4);
        _bossFill.offsetMax = new Vector2(-4, -4);

        _bossBar.SetActive(false);
    }

    private void Update()
    {
        UpdateInfo();

        //ボスHPバーを滑らかに減らす
        if (_bossBar.activeSelf)
        {
            Vector2 max = _bossFill.anchorMax;
            max.x = Mathf.MoveTowards(max.x, _bossFillTarget, Time.unscaledDeltaTime * 1.5f);
            _bossFill.anchorMax = max;
        }

        //電車の警告を点滅
        UpdateWarn(_warnLeft, ref _warnLeftTime);
        UpdateWarn(_warnRight, ref _warnRightTime);
    }

    private void UpdateInfo()
    {
        if (!_player || !_gameManager) return;
        if (_gameManager._currentState != GameManager.GameState.Playing) return;

        if (!_isStarted)
        {
            _isStarted = true;
            _startZ = _player.position.z;
        }

        if (_bossBattle)
        {
            if (_bossBattle.InBattle)
                _info.text = $"BOSS {_bossBattle.BossNumber}/{_bossBattle.BossCount}";
            else if (_bossBattle.BossNumber <= _bossBattle.BossCount)
                _info.text = $"NEXT BOSS  {Mathf.CeilToInt(_bossBattle.DistanceToNextBoss):N0}m\n<size=70%>BOSS {_bossBattle.BossNumber}/{_bossBattle.BossCount}</size>";
        }
        else
        {
            float distance = Mathf.Max(0f, _player.position.z - _startZ);
            float best = PlayerPrefs.GetFloat(PlayModeInfo.HighScoreKey, 0f);
            _info.text = $"{Mathf.FloorToInt(distance):N0}m\n<size=70%>BEST {Mathf.FloorToInt(best):N0}</size>";
        }
    }

    private void UpdateWarn(TextMeshProUGUI text, ref float time)
    {
        if (time <= 0f)
        {
            if (text.gameObject.activeSelf) text.gameObject.SetActive(false);
            return;
        }

        time -= Time.unscaledDeltaTime;
        if (!text.gameObject.activeSelf) text.gameObject.SetActive(true);
        text.alpha = Mathf.PingPong(Time.unscaledTime * 6f, 1f) > 0.3f ? 1f : 0.2f;
    }

    // ===============================
    // 外から呼ぶ
    // ===============================

    /// <summary>
    /// 画面中央に大きく出す
    /// </summary>
    public void Banner(string message, Color color, float duration = 1.6f)
    {
        if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
        _bannerCoroutine = StartCoroutine(BannerRoutine(message, color, duration));
    }

    private IEnumerator BannerRoutine(string message, Color color, float duration)
    {
        _banner.gameObject.SetActive(true);
        _banner.text = message;
        _banner.color = color;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            //最初にドンと大きく出て、最後にふっと消える
            float pop = Mathf.Clamp01(t / 0.15f);
            _banner.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, EaseOut(pop));
            _banner.alpha = Mathf.Clamp01((duration - t) / 0.3f);
            yield return null;
        }

        _banner.gameObject.SetActive(false);
        _bannerCoroutine = null;
    }

    /// <summary>
    /// 少し小さめの文字を下から浮かせて出す（アイテム取得など）
    /// </summary>
    public void Popup(string message, Color color)
    {
        StartCoroutine(PopupRoutine(message, color, _popupCount++ % 3));
    }

    private IEnumerator PopupRoutine(string message, Color color, int slot)
    {
        TextMeshProUGUI text = CreateText("Popup", _root, 56, TextAlignmentOptions.Center);
        Vector2 basePos = new Vector2(0, -60 - slot * 70);
        SetRect(text.rectTransform, new Vector2(0.5f, 0.45f), new Vector2(1200, 80), new Vector2(0.5f, 0.5f), basePos);
        text.text = message;
        text.color = color;

        const float duration = 1.2f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            text.rectTransform.anchoredPosition = basePos + Vector2.up * (EaseOut(Mathf.Clamp01(t / duration)) * 80f);
            text.transform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, EaseOut(Mathf.Clamp01(t / 0.12f)));
            text.alpha = Mathf.Clamp01((duration - t) / 0.35f);
            yield return null;
        }

        Destroy(text.gameObject);
    }

    /// <summary>
    /// 画面全体を一瞬光らせる
    /// </summary>
    public void Flash(Color color, float duration = 0.25f)
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            Color c = color;
            c.a = color.a * (1f - t / duration);
            _flash.color = c;
            yield return null;
        }
        _flash.color = new Color(1, 1, 1, 0);
        _flashCoroutine = null;
    }

    /// <summary>
    /// ボスHPバーを出す・更新する
    /// </summary>
    public void ShowBossBar(string bossName, int hp, int maxHp)
    {
        _bossBar.SetActive(true);
        _bossName.text = bossName;
        _bossFillTarget = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;

        //出したばかりのときは満タンから
        if (hp == maxHp)
        {
            Vector2 max = _bossFill.anchorMax;
            max.x = 1f;
            _bossFill.anchorMax = max;
        }
    }

    public void HideBossBar()
    {
        _bossBar.SetActive(false);
    }

    /// <summary>
    /// 電車が来る方向を知らせる（fromLeft: 左から来る）
    /// </summary>
    public void WarnTrain(bool fromLeft, float duration = 1.5f)
    {
        if (fromLeft) _warnLeftTime = duration;
        else _warnRightTime = duration;
    }

    // ===============================
    // UI を作る道具
    // ===============================

    private static float EaseOut(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = align;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        return text;
    }

    private static Image CreateImage(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        SetRect(rect, anchor, size, new Vector2(0.5f, 0.5f), Vector2.zero);
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 pivot, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
