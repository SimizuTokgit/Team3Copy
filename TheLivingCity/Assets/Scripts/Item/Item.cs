using UnityEngine;

//安井
//強化アイテム
public abstract class Item : MonoBehaviour
{
    [SerializeField] protected int UP_NUMBER; //どれだけ上げるか
    [SerializeField] public ParticleSystem ItemEffect;
    [SerializeField] public AudioClip ItemSE;

    public abstract void UpGrade(PlayerStatus playerStatus);
}
