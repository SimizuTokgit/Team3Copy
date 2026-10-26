using System.Collections.Generic;
using UnityEngine;

//清水
//道に置く障害物の一覧（カタログ）
//マップのプレハブを手で組むかわりに、ここに障害物を1つずつ登録しておくと、地面ができるたびに自動で並べる
//・新しい障害物を足す：Entries に1行足してプレハブ（モデルそのままでもよい）を入れるだけ
//・出やすさ：Weight、数：Count〜、間隔：MinSpacing
//・置いた後は ChunkLayout が「必ず通れる隙間」を空けるので、詰む並びにはならない
//Project ウィンドウで 右クリック → Create → TheLivingCity → Obstacle Catalog で新しく作れる
[CreateAssetMenu(menuName = "TheLivingCity/Obstacle Catalog", fileName = "ObstacleCatalog")]
public class ObstacleCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string Name;
        public GameObject Prefab;
        [Min(0)] public int Weight = 10;                    //出やすさ
        [Tooltip("HPの減りの倍率。-1 なら大きさで自動（細い物は小さく）")]
        public float DamageRate = -1f;
        public Vector2 ScaleRange = Vector2.one;            //大きさのばらつき（最小, 最大）
        [Tooltip("向きを何度ずつ回すか。0 なら回さない、360 なら完全にランダム")]
        public float YawStep = 90f;
        public float HeightOffset = 0f;                     //地面からの高さのずれ（埋めたいときはマイナス）
        [Tooltip("感電触手のボス戦（中ボス2）のときだけ出す（触手が巻き付いた物など）")]
        public bool ElectricBattleOnly = false;
    }

    public List<Entry> Entries = new List<Entry>();

    [Header("地面1枚（96m）に置く数")]
    public int CountEasy = 8;
    public int CountNormal = 12;
    public int CountHard = 15;

    [Header("置き方")]
    public float MinSpacing = 7f;   //障害物どうしの最低の間隔
    public float EdgeMargin = 3f;   //道の端からどれだけ離すか

    public int GetCount(GenerateObstacle.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case GenerateObstacle.Difficulty.Easy: return CountEasy;
            case GenerateObstacle.Difficulty.Hard: return CountHard;
            default: return CountNormal;
        }
    }

    /// <summary>
    /// 出やすさに合わせて1つ選ぶ
    /// </summary>
    public Entry Pick(bool electricBattle = false)
    {
        int total = 0;
        foreach (Entry e in Entries)
        {
            if (CanUse(e, electricBattle)) total += e.Weight;
        }
        if (total <= 0) return null;

        int r = Random.Range(0, total);
        foreach (Entry e in Entries)
        {
            if (!CanUse(e, electricBattle)) continue;
            if (r < e.Weight) return e;
            r -= e.Weight;
        }
        return null;
    }

    //清水 感電触手のボス戦でだけ出す物は、それ以外では選ばない
    private static bool CanUse(Entry e, bool electricBattle)
    {
        return e != null && e.Prefab && (electricBattle || !e.ElectricBattleOnly);
    }
}
