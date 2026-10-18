using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    [System.Serializable]
    public class ItemUIData
    {
        //アイテム名
        public string itemName;

        //UI
        public Sprite prefab;
    }

    //アイテムUI一覧
    [SerializeField]
    private ItemUIData[] items;

    [SerializeField]
    private Image[] LeftRightImage;


    [SerializeField]
    private List<string> Leftstrings;
    [SerializeField]
    private string Rightstring;

    public void ItemNumberCheck()
    {
        Leftstrings.Clear();

        int LeftNumber= Random.Range(0, items.Length);
        int LeftNumber1= Random.Range(0, items.Length);
        int RightNumber = Random.Range(0, items.Length);


        LeftRightImage[0].sprite = items[LeftNumber].prefab;
        LeftRightImage[1].sprite = items[LeftNumber1].prefab;
        LeftRightImage[2].sprite = items[RightNumber].prefab;

        Leftstrings.Add(items[LeftNumber].itemName);
        Leftstrings.Add(items[LeftNumber1].itemName);
        Rightstring = items[RightNumber].itemName;
    }

    public string LeftItemName1()
    {
        return Leftstrings[0];
    }
    public string LeftItemName2()
    {
        return Leftstrings[1];
    }
    public string RightItemName()
    {
        return Rightstring;
    }
}
