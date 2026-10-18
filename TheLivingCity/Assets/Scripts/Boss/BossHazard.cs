using System.Collections.Generic;
using UnityEngine;

//清水
//ボス戦中に出したギミックの目印
//ボスを倒したときにまとめて消すために使う
public class BossHazard : MonoBehaviour
{
    private static readonly HashSet<BossHazard> hazards = new HashSet<BossHazard>();

    private void OnEnable()
    {
        hazards.Add(this);
    }

    private void OnDisable()
    {
        hazards.Remove(this);
    }

    /// <summary>
    /// 出ているボスギミックをすべて消す
    /// </summary>
    public static void DestroyAll()
    {
        foreach (BossHazard hazard in new List<BossHazard>(hazards))
        {
            if (hazard) Destroy(hazard.gameObject);
        }
        hazards.Clear();
    }

    /// <summary>
    /// 触れたら一撃死になるようにする（Deadレイヤー＋トリガー）
    /// </summary>
    public static void MakeDeadly(GameObject root)
    {
        int deadLayer = LayerMask.NameToLayer("Dead");
        if (deadLayer < 0)
        {
            Debug.LogWarning("Deadレイヤーがありません。Tags and Layers に Dead を追加してください");
            return;
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = deadLayer;
        }

        //PlayerCollider は OnTriggerEnter で判定しているのでトリガーにする
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            col.isTrigger = true;
        }
    }
}
