using System.Collections.Generic;
using UnityEngine;

//清水
//元の障害物と「電撃の触手が巻き付いた版」の対応表
//CGさんから新しいモデルが届いたら、ここに1行足すだけで感電触手のボス戦で入れ替わる
//表に無い障害物は、FallbackTentacle（ふつうの触手）を巻き付けて感電させる
[CreateAssetMenu(fileName = "ElectricVariantTable", menuName = "TheLivingCity/Electric Variant Table")]
public class ElectricVariantTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string Name;           //メモ用
        public GameObject Original;   //元の障害物（空なら使わない。元のモデルが届くまでの置き場）
        public GameObject Electric;   //触手が巻き付いた版（触れたら一撃死）
    }

    public List<Entry> Entries = new List<Entry>();

    [Header("専用のモデルが無い障害物")]
    public GameObject FallbackTentacle;          //専用モデルが無い物は、この触手を巻き付けて感電させる（空なら入れ替えない）
    public int FallbackTentacleCount = 2;        //1つの障害物に巻き付ける本数
    public float FallbackTentacleHeight = 1.15f; //障害物の高さの何倍の長さにするか

    [System.NonSerialized] private float _tentacleHeight = -1f;

    /// <summary>
    /// root の下にある障害物を、chance の確率で感電させる
    /// 表に専用モデル（触手が巻き付いた版）があればそれに入れ替え、無ければ FallbackTentacle を巻き付ける
    /// canSwap で入れ替えてよいかを決める（必ず通れる道の上は入れ替えない、など）
    /// 感電させた物を返す
    /// </summary>
    public List<GameObject> SwapIn(Transform root, float chance, System.Func<Transform, bool> canSwap)
    {
        List<GameObject> swapped = new List<GameObject>();
        Dictionary<string, Entry> byName = new Dictionary<string, Entry>();
        foreach (Entry e in Entries)
        {
            if (e != null && e.Original && e.Electric) byName[e.Original.name] = e;
        }

        //先に全部集めてから入れ替える（入れ替えながら探すと、新しく置いた物まで見てしまう）
        List<(Transform, Entry)> targets = new List<(Transform, Entry)>();
        Collect(root, byName, LayerMask.NameToLayer("Obstacle"), FallbackTentacle != null, targets);

        foreach ((Transform t, Entry e) in targets)
        {
            if (!t || Random.value >= chance) continue;
            if (canSwap != null && !canSwap(t)) continue;

            GameObject obj = e != null ? Replace(t, e) : WrapTentacles(t.gameObject);
            if (obj) swapped.Add(obj);
        }
        return swapped;
    }

    //専用モデルに入れ替える
    private static GameObject Replace(Transform t, Entry e)
    {
        GameObject obj = Instantiate(e.Electric, t.parent);
        obj.name = e.Electric.name;
        obj.transform.localPosition = t.localPosition;
        obj.transform.localRotation = t.localRotation;

        //カタログで置いた物は大きさがばらばらなので、元のプレハブとの比をそのまま使う
        float baseScale = Mathf.Max(0.0001f, e.Original.transform.localScale.x);
        obj.transform.localScale = e.Electric.transform.localScale * (t.localScale.x / baseScale);

        Destroy(t.gameObject);
        return obj;
    }

    //専用モデルが無い物は、まわりに触手を巻き付けて感電させる
    private GameObject WrapTentacles(GameObject target)
    {
        if (!TryGetBounds(target, out Bounds b)) return null;

        //触手の長さを測る（最初の1回だけ、プレハブの大きさのまま1本出して測る）
        if (_tentacleHeight <= 0f)
        {
            GameObject probe = Instantiate(FallbackTentacle, new Vector3(0f, -500f, 0f), Quaternion.identity);
            _tentacleHeight = TryGetBounds(probe, out Bounds tb) ? Mathf.Max(0.1f, tb.size.y) : 10f;
            DestroyImmediate(probe);
        }
        float scale = b.size.y * FallbackTentacleHeight / _tentacleHeight;

        int count = Mathf.Max(1, FallbackTentacleCount);
        float startAngle = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            //障害物のまわりに等間隔に立てて、少し内側にかたむけて巻き付いて見せる
            float angle = startAngle + 360f * i / count;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 edge = new Vector3(dir.x * b.extents.x, 0f, dir.z * b.extents.z) * 0.85f;
            Vector3 pos = new Vector3(b.center.x, b.min.y, b.center.z) + edge;

            GameObject tentacle = Instantiate(FallbackTentacle, pos, Quaternion.LookRotation(-dir) * Quaternion.Euler(12f, Random.Range(-40f, 40f), 0f));
            tentacle.transform.localScale = FallbackTentacle.transform.localScale * scale;
            //当たり判定は障害物のものを使う（触手が振れて当たると分かりにくいので）
            foreach (Collider col in tentacle.GetComponentsInChildren<Collider>()) Destroy(col);
            tentacle.transform.SetParent(target.transform, true);
        }

        target.AddComponent<ElectricHazard>();
        return target;
    }

    private static bool TryGetBounds(GameObject obj, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    //表にある物（名前が同じ物）と、障害物レイヤーの物を集める。Entry が null の物は触手を巻き付ける
    private static void Collect(Transform parent, Dictionary<string, Entry> byName, int obstacleLayer, bool useFallback, List<(Transform, Entry)> targets)
    {
        foreach (Transform child in parent)
        {
            //ボスのギミックとジャンプ台（Highway を傾けた物）、もう感電している物はさわらない
            if (child.GetComponent<BossHazard>() || child.GetComponent<BossRamp>() || child.GetComponent<ElectricHazard>()) continue;

            if (byName.TryGetValue(BaseName(child.name), out Entry e))
            {
                targets.Add((child, e));
                continue; //中まで見ない
            }
            if (useFallback && child.gameObject.layer == obstacleLayer && child.GetComponentInChildren<Collider>())
            {
                //マッププレハブの入れ物ではなく、障害物1つ（カタログで置いた物か、子が無い物）
                if (child.GetComponent<ObstacleInfo>() || child.childCount == 0 || child.GetComponent<Collider>())
                {
                    targets.Add((child, null));
                    continue;
                }
            }
            Collect(child, byName, obstacleLayer, useFallback, targets);
        }
    }

    /// <summary>
    /// "Highway(Clone)" や "Highway (2)" を "Highway" にする
    /// </summary>
    public static string BaseName(string name)
    {
        name = name.Replace("(Clone)", "").Trim();
        if (name.EndsWith(")"))
        {
            int open = name.LastIndexOf(" (");
            if (open > 0 && int.TryParse(name.Substring(open + 2, name.Length - open - 3), out _)) name = name.Substring(0, open);
        }
        return name.Trim();
    }
}
