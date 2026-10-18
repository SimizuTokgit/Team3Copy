using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 数字を画像で並べて表示する
public class NumberImage : MonoBehaviour
{
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];  // 0から9の画像
    [SerializeField] private Sprite[] warningSprites = new Sprite[10]; // 注意表示のときの0から9の画像。空でもよい
    [SerializeField] private Sprite periodSprite;                      // 小数点の画像。Timer_Colon を入れてもよい
    [SerializeField] private Vector2 digitSize = new Vector2(233, 233);
    [SerializeField] private float digitSpace = 150; // 数字の間隔
    [SerializeField] private float periodSpace = 70; // 小数点の間隔

    [SerializeField, Range(0, 3)] private int decimals = 0; // 小数第何位まで出すか。0なら整数のまま

    // warningSprites を用意していないときは、代わりにこの色に変えて注意を伝える
    [SerializeField] private Color warningColor = new Color(1f, 0.28f, 0.22f);

    private readonly List<Image> digitImages = new List<Image>();
    private string text = null;
    private bool isWarning = false;

    // 今表示している文字列。NumberReveal が完成形を読むのに使う
    public string CurrentText => text;

    public void SetNumber(int value)
    {
        SetText(Mathf.Max(value, 0).ToString());
    }

    // 残り時間のように小数を持つ値はこちら。decimalsが0なら整数表示になる
    public void SetNumber(float value)
    {
        value = Mathf.Max(value, 0f);

        if (decimals <= 0 || !periodSprite)
        {
            SetText(Mathf.FloorToInt(value).ToString());
            return;
        }

        // 切り上げずに切り捨てる。カウントダウンは 9→8→…→0 と落ちていくほうが自然
        float scale = Mathf.Pow(10f, decimals);
        value = Mathf.Floor(value * scale + 0.0001f) / scale;

        SetText(value.ToString("F" + decimals));
    }

    // 数字と '.' のほかに ' '（幅だけ取って何も出さない）が使える。一桁ずつ出す演出用
    public void ShowRawText(string value)
    {
        SetText(value);
    }

    // i 文字目の画像。1文字だけ動かしたり色を変えたりしたいとき用
    public Image GetDigitImage(int index)
    {
        if (index < 0 || index >= digitImages.Count) return null;
        return digitImages[index];
    }

    public void SetWarning(bool warning)
    {
        if (warning == isWarning) return;

        isWarning = warning;
        if (text != null) UpdateImages();
    }

    private void SetText(string newText)
    {
        if (newText == text) return;

        text = newText;
        UpdateImages();
    }

    private void UpdateImages()
    {
        // 注意表示用の画像が10個そろっていればそれを使う。無ければ色だけ変える
        bool useWarningSprites = isWarning && warningSprites.Length >= 10;
        Sprite[] sprites = useWarningSprites ? warningSprites : numberSprites;
        Color color = (isWarning && !useWarningSprites) ? warningColor : Color.white;

        // 足りない桁の画像を作る
        while (digitImages.Count < text.Length)
        {
            GameObject digit = new GameObject("Digit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            digit.layer = gameObject.layer;
            digit.transform.SetParent(transform, false);

            Image image = digit.GetComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = digitSize;
            digitImages.Add(image);
        }

        // 小数点は幅が違うので、1文字ずつ幅を足して中央揃えにする
        float totalWidth = 0;
        for (int i = 0; i < text.Length; i++) totalWidth += GetSpace(text[i]);

        float x = -totalWidth * 0.5f;
        for (int i = 0; i < digitImages.Count; i++)
        {
            if (i >= text.Length)
            {
                digitImages[i].gameObject.SetActive(false);
                continue;
            }

            float space = GetSpace(text[i]);
            Sprite sprite = GetSprite(text[i], sprites);

            // 空白や画像の無い文字も幅だけは取る。一桁ずつ出したときに位置がずれないように
            digitImages[i].gameObject.SetActive(sprite != null);
            if (sprite)
            {
                digitImages[i].sprite = sprite;
                digitImages[i].color = color;
                digitImages[i].rectTransform.sizeDelta = GetSize(text[i], sprite);
            }

            digitImages[i].rectTransform.anchoredPosition = new Vector2(x + space * 0.5f, 0);
            x += space;
        }
    }

    private Sprite GetSprite(char c, Sprite[] sprites)
    {
        if (c == '.') return periodSprite;

        int index = c - '0';
        return (index >= 0 && index < sprites.Length) ? sprites[index] : null;
    }

    private float GetSpace(char c)
    {
        return c == '.' ? periodSpace : digitSpace;
    }

    private Vector2 GetSize(char c, Sprite sprite)
    {
        // 数字はこれまで通り固定。小数点だけ縦を合わせて横は画像の比率に合わせる
        if (c != '.') return digitSize;

        float height = sprite.rect.height > 0 ? sprite.rect.height : 1f;
        return new Vector2(digitSize.y * sprite.rect.width / height, digitSize.y);
    }
}
