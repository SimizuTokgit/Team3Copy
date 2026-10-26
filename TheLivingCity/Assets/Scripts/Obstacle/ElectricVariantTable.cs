using System.Collections.Generic;
using UnityEngine;

//清水
//元の障害物と「電撃の触手が巻き付いた版」の対応表
//CGさんから新しいモデルが届いたら、ここに1行足すだけで感電触手のボス戦で入れ替わる
[CreateAssetMenu(fileName = "ElectricVariantTable", menuName = "TheLivingCity/Electric Variant Table")]
public class ElectricVariantTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string Name;           //メモ用
        public GameObject Original;   //元の障害物（空なら入れ替えはせず、ボス戦で置くだけ）
        public GameObject Electric;   //触手が巻き付いた版（触れたら一撃死）
    }

    public List<Entry> Entries = new List<Entry>();

    /// <summary>
    /// 触手が巻き付いた版をぜんぶ返す（ボス戦で置く物の候補）
    /// </summary>
    public List<GameObject> GetElectricPrefabs()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (Entry e in Entries)
        {
            if (e != null && e.Electric) list.Add(e.Electric);
        }
        return list;
    }

    /// <summary>
    /// root の下にある元の障害物を、chance の確率で触手が巻き付いた版に入れ替える
    /// canSwap で入れ替えてよいかを決める（必ず通れる道の上は入れ替えない、など）
    /// 入れ替えてできた物を返す
    /// </summary>
    public List<GameObject> SwapIn(Transform root, float chance, System.Func<Transform, bool> canSwap)
    {
        List<GameObject> swapped = new List<GameObject>();
        Dictionary<string, Entry> byName = new Dictionary<string, Entry>();
        foreach (Entry e in Entries)
        {
            if (e != null && e.Original && e.Electric) byName[e.Original.name] = e;
        }
        if (byName.Count == 0) return swapped;

        //先に全部集めてから入れ替える（入れ替えながら探すと、新しく置いた物まで見てしまう）
        List<(Transform, Entry)> targets = new List<(Transform, Entry)>();
        Collect(root, byName, targets);

        foreach ((Transform t, Entry e) in targets)
        {
            if (!t || Random.value >= chance) continue;
            if (canSwap != null && !canSwap(t)) continue;

            GameObject obj = Instantiate(e.Electric, t.parent);
            obj.name = e.Electric.name;
            obj.transform.localPosition = t.localPosition;
            obj.transform.localRotation = t.localRotation;

            //カタログで置いた物は大きさがばらばらなので、元のプレハブとの比をそのまま使う
            float baseScale = Mathf.Max(0.0001f, e.Original.transform.localScale.x);
            obj.transform.localScale = e.Electric.transform.localScale * (t.localScale.x / baseScale);

            Destroy(t.gameObject);
            swapped.Add(obj);
        }
        return swapped;
    }

    private static void Collect(Transform parent, Dictionary<string, Entry> byName, List<(Transform, Entry)> targets)
    {
        foreach (Transform child in parent)
        {
            //ボスのギミックとジャンプ台（Highway を傾けた物）はさわらない
            if (child.GetComponent<BossHazard>() || child.GetComponent<BossRamp>()) continue;

            if (byName.TryGetValue(BaseName(child.name), out Entry e))
            {
                targets.Add((child, e));
                continue; //中まで見ない
            }
            Collect(child, byName, targets);
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
