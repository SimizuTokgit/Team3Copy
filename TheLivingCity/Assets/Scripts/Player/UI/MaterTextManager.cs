using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MaterText
{
    public List<Sprite> images = new List<Sprite>(10); // 9ñáÇÃImageÇï€éù
}

public class MaterTextManager : MonoBehaviour
{
    [SerializeField]
    private List<MaterText> materTextSets = new List<MaterText>(4); // 4ÉZÉbÉg

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
