using UnityEngine;

//清水
//障害物1つ分の情報。ぶつかったときの HP の減り方を決める
//減速はどの障害物でも同じで、HP の減りだけをこの倍率で変える
//付いていない障害物は倍率1（今まで通り）
public class ObstacleInfo : MonoBehaviour
{
    [Tooltip("HPの減りの倍率。-1 なら大きさで自動（細い物は小さく、それ以外は1）")]
    public float DamageRate = -1f;

    [Tooltip("木・看板・電柱などの細い物（ChunkLayout が大きさを見て自動で付ける）")]
    public bool IsSmall;

    public float GetDamageRate()
    {
        return DamageRate >= 0f ? DamageRate : 1f;
    }
}
