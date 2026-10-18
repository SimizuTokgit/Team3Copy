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

    public void TextUpdate(int velocity,int OneMaterSpeed)
    {
        var imageNum = velocity / OneMaterSpeed;

        int velocityOnes = velocity % 10;
        int velocityTens = velocity / 10;

        onesPlaceImage.sprite = materTextSets[imageNum].images[velocityOnes];
        tensPlaceImage.sprite = materTextSets[imageNum].images[velocityTens];

    }
}
