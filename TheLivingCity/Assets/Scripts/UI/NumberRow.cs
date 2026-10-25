using System.Collections.Generic;
using TMPro;
using UnityEngine;

//清水
//文字と数字画像を1行に並べて出す
//  例）row.Set("NEXT BOSS ", 1250, "m")  → 「NEXT BOSS 」は文字、1250 は NumberImage の画像、「m」は文字
//string は文字、int / float は数字画像になる。数字の画像セット（NumberFont）が無いときは数字も文字で出す
public class NumberRow : MonoBehaviour
{
    public NumberFont Font;
    public float NumberHeight = 64f;   //数字画像の高さ
    public float FontSize = 48f;       //文字の大きさ
    public float Gap = 6f;             //部品どうしの間
    [Range(0f, 1f)] public float Align = 0.5f; //0 で左ぞろえ、0.5 で中央、1 で右ぞろえ

    private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
    private readonly List<NumberImage> _numbers = new List<NumberImage>();
    private Color _color = Color.white;

    /// <summary>
    /// 文字（string）と数字（int / float）を順に並べる
    /// </summary>
    public void Set(params object[] parts)
    {
        int textIndex = 0;
        int numberIndex = 0;
        List<RectTransform> items = new List<RectTransform>();
        List<float> widths = new List<float>();

        foreach (object part in parts)
        {
            if (part == null) continue;

            bool isNumber = part is int || part is float;
            if (isNumber && Font)
            {
                NumberImage number = GetNumber(numberIndex++);
                if (part is int intValue) number.SetNumber(intValue);
                else number.SetNumber((float)part);
                number.SetColor(_color);
                items.Add((RectTransform)number.transform);
                widths.Add(number.Width);
            }
            else
            {
                TextMeshProUGUI text = GetText(textIndex++);
                text.text = isNumber ? (part is int textValue ? textValue.ToString() : Mathf.FloorToInt((float)part).ToString()) : part.ToString();
                text.color = _color;
                text.ForceMeshUpdate();
                float w = text.preferredWidth;
                text.rectTransform.sizeDelta = new Vector2(w, FontSize * 1.3f);
                items.Add(text.rectTransform);
                widths.Add(w);
            }
        }

        //使わなかった部品は隠す
        for (int i = textIndex; i < _texts.Count; i++) _texts[i].gameObject.SetActive(false);
        for (int i = numberIndex; i < _numbers.Count; i++) _numbers[i].gameObject.SetActive(false);

        //全体の幅を出して並べる
        float total = 0f;
        foreach (float w in widths) total += w;
        total += Gap * Mathf.Max(0, widths.Count - 1);

        float x = -total * Align;
        for (int i = 0; i < items.Count; i++)
        {
            items[i].anchoredPosition = new Vector2(x + widths[i] * 0.5f, 0f);
            x += widths[i] + Gap;
        }
    }

    /// <summary>
    /// 色を変える（透明度も効く。フェードに使う）
    /// </summary>
    public void SetColor(Color color)
    {
        _color = color;
        foreach (TextMeshProUGUI text in _texts)
        {
            if (text) text.color = color;
        }
        foreach (NumberImage number in _numbers)
        {
            if (number) number.SetColor(color);
        }
    }

    private TextMeshProUGUI GetText(int index)
    {
        while (_texts.Count <= index)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = FontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color32(0, 0, 0, 255);
            _texts.Add(text);
        }
        _texts[index].gameObject.SetActive(true);
        return _texts[index];
    }

    private NumberImage GetNumber(int index)
    {
        while (_numbers.Count <= index)
        {
            GameObject go = new GameObject("Number", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            ((RectTransform)go.transform).sizeDelta = Vector2.zero;
            NumberImage number = go.AddComponent<NumberImage>();
            number.Configure(Font, NumberHeight);
            _numbers.Add(number);
        }
        _numbers[index].gameObject.SetActive(true);
        return _numbers[index];
    }
}
