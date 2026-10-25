using UnityEngine;

//安井
//強化アイテム
public abstract class Item : MonoBehaviour
{
    [SerializeField] protected int UP_NUMBER; //どれだけ上げるか
    [SerializeField] public ParticleSystem ItemEffect;
    [SerializeField] public AudioClip ItemSE;

    public abstract void UpGrade(PlayerStatus playerStatus);

    //清水 取ったときに画面に出す文字と数字（取った後のステータスの値。数字は画像で出る）と色
    public abstract string PopupLabel { get; }
    public abstract int GetPopupValue(PlayerStatus playerStatus);
    public virtual Color PopupColor => GameHUD.Gold;
}
