using UnityEngine;

//清水
//どうやってやられたか。ゲームオーバーの演出（シーン）を変えるのに使う
//プレス機 → Press、ガスバーナー → Burner、電撃 → Shock、それ以外（HP切れ・落下・時間切れ・電車など）→ いつものシーン
public static class GameOverCause
{
    public enum Cause
    {
        Normal,
        Press,
        Burner,
        Shock,
    }

    public static Cause Current { get; private set; } = Cause.Normal;

    public static void Set(Cause cause)
    {
        Current = cause;
    }

    public static void Reset()
    {
        Current = Cause.Normal;
    }

    /// <summary>
    /// 当たった一撃死の物から、やられ方を決める
    /// </summary>
    public static Cause FromHazard(Collider other)
    {
        if (!other) return Cause.Normal;
        if (other.GetComponentInParent<PressHazard>()) return Cause.Press;
        if (other.GetComponentInParent<BurnerHazard>()) return Cause.Burner;
        if (other.GetComponentInParent<ElectricHazard>()) return Cause.Shock;
        return Cause.Normal;
    }

    /// <summary>
    /// やられ方に合わせたゲームオーバーのシーン名
    /// </summary>
    public static string SceneName
    {
        get
        {
            switch (Current)
            {
                case Cause.Press: return "GameOverScenePress";
                case Cause.Burner: return "GameOverSceneBurner";
                case Cause.Shock: return "GameOverSceneShock";
                default: return "GameOverScene";
            }
        }
    }
}
