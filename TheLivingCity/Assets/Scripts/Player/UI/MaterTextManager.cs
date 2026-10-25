using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MaterText
{
    public List<Sprite> images = new List<Sprite>(10); // 9枚のImageを保持
}

public class MaterTextManager : MonoBehaviour
{
    [SerializeField]
    private List<MaterText> materTextSets = new List<MaterText>(4); // 4セット

    [SerializeField] private Image onesPlaceImage;
    [SerializeField] private Image tensPlaceImage;

    //清水 100以上のときの百の位。無ければ十の位の画像を複製して左に並べる
    [SerializeField] private Image hundredsPlaceImage;

    public void TextUpdate(int velocity,int OneMaterSpeed)
    {
        velocity = Mathf.Max(0, velocity);

        //清水 色のセットは最後のものより先に行かない（今までは80以上でエラーになって79で止まって見えた）
        var imageNum = Mathf.Min(velocity / Mathf.Max(1, OneMaterSpeed), materTextSets.Count - 1);

        int velocityOnes = velocity % 10;
        int velocityTens = (velocity / 10) % 10;
        int velocityHundreds = velocity / 100;

        onesPlaceImage.sprite = materTextSets[imageNum].images[velocityOnes];
        tensPlaceImage.sprite = materTextSets[imageNum].images[velocityTens];

        Image hundreds = GetHundredsImage(velocityHundreds > 0);
        if (hundreds)
        {
            hundreds.gameObject.SetActive(velocityHundreds > 0);
            if (velocityHundreds > 0) hundreds.sprite = materTextSets[imageNum].images[Mathf.Min(velocityHundreds, 9)];
        }
    }

    private Image GetHundredsImage(bool needed)
    {
        if (hundredsPlaceImage || !needed) return hundredsPlaceImage;

        //十の位と一の位の間隔だけ左に、十の位を複製して置く
        hundredsPlaceImage = Instantiate(tensPlaceImage, tensPlaceImage.transform.parent);
        hundredsPlaceImage.name = "HundredsPlace";
        Vector2 step = tensPlaceImage.rectTransform.anchoredPosition - onesPlaceImage.rectTransform.anchoredPosition;
        hundredsPlaceImage.rectTransform.anchoredPosition = tensPlaceImage.rectTransform.anchoredPosition + step;
        return hundredsPlaceImage;
    }
}
