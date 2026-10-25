using UnityEngine;

//清水
//数字の画像セット（0〜9と小数点）
//NumberImage / NumberRow にまとめて渡して、同じ見た目の数字を使い回すためのもの
//Project ウィンドウで 右クリック → Create → TheLivingCity → Number Font で作れる
[CreateAssetMenu(menuName = "TheLivingCity/Number Font", fileName = "NumberFont")]
public class NumberFont : ScriptableObject
{
    public Sprite[] Digits = new Sprite[10]; //0から9の画像
    public Sprite Period;                    //小数点（無くてもよい）

    [Tooltip("数字の間隔（数字の幅に対する割合）。1で幅ぴったり")]
    public float Spacing = 0.9f;
}
