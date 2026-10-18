using UnityEngine;

//安井
//ダメージを受けた時の演出
//清水 Post Processing Stack v2 は URP では画面に出ないので、ScreenEffects（URP の Volume）で出すようにした
public class DamageScreen : MonoBehaviour
{
    [SerializeField] private float DAMAGE_STRENGTH = 1f; //赤枠・揺れの強さ

    /// <summary>
    /// ダメージを受ける演出
    /// </summary>
    public void Damage()
    {
        ScreenEffects.Get().Damage(DAMAGE_STRENGTH);
    }
}
