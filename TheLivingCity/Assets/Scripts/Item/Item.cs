using UnityEngine;

//安井
//強化アイテム
public abstract class Item : MonoBehaviour
{
    [SerializeField] protected int UP_NUMBER; //どれだけ上げるか
    [SerializeField] public ParticleSystem ItemEffect;
    [SerializeField] public AudioClip ItemSE;

    public abstract void UpGrade(PlayerStatus playerStatus);

    //清水 取ったときに画面に出す文字と色（取った後のステータスを渡す）
    public abstract string GetPopupText(PlayerStatus playerStatus);
    public virtual Color PopupColor => GameHUD.Gold;
}
