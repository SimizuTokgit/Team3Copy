using UnityEngine;

//à¿à‰
//ã≠âªÉAÉCÉeÉÄ
public abstract class Item : MonoBehaviour
{
    [SerializeField] protected int UP_NUMBER; //Ç«ÇÍÇæÇØè„Ç∞ÇÈÇ©
    [SerializeField] public ParticleSystem ItemEffect;
    [SerializeField] public AudioClip ItemSE;

    public abstract void UpGrade(PlayerStatus playerStatus);
}
