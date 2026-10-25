using UnityEngine;

//安井
//強化アイテム
public abstract class Item : MonoBehaviour
{
    [SerializeField] protected float UP_NUMBER; //どれだけ上げるか（0.5 のような小数も使える）
    [SerializeField] public ParticleSystem ItemEffect;
    [SerializeField] public AudioClip ItemSE;

    public abstract void UpGrade(PlayerStatus playerStatus);

    //清水 取ったときに画面に出す文字と数字（取った後のステータスの値。数字は画像で出る）と色
    public abstract string PopupLabel { get; }
    public abstract float GetPopupValue(PlayerStatus playerStatus);
    public virtual Color PopupColor => GameHUD.Gold;
}
