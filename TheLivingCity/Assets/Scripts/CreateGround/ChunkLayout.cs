using System.Collections.Generic;
using UnityEngine;

//清水
//地面1枚に置いた障害物を調べて、遊びやすく整える
//・細い障害物（木・電柱・標識の柱など）は当たり判定を消して飾りにする（よけるのが難しすぎた）
//・前の地面から続く「必ず通れる道」を1本決めて、そこをふさぐ障害物をどける
//・アイテムを置ける場所（障害物の中ではない場所）を教える
public class ChunkLayout
{
    //当たり判定1つ分（地面1枚のローカル座標）
    private class Block
    {
        public Transform Unit;   //どけるときに消すまとまり（マップの中の障害物1つ）
        public Collider Collider;
        public Vector3 Min;
        public Vector3 Max;
    }

    private readonly GroundAutomatically _chunk;
    private readonly List<Block> _blocks = new List<Block>();
    private readonly List<Rect> _noItemAreas = new List<Rect>(); //アイテムを置かない場所（ジャンプ台の下など）

    public float CorridorStartX { get; private set; }
    public float CorridorEndX { get; private set; }
    public float CorridorWidth { get; private set; }

    //この太さより細い障害物は飾りにする（m）
    public const float SmallSize = 1.2f;
    //プレイヤーが走る高さ（地面からこの高さまでにある物がじゃまになる）
    private const float RunHeight = 3.5f;

    public ChunkLayout(GroundAutomatically chunk)
    {
        _chunk = chunk;
    }

    /// <summary>
    /// 地面の子にある障害物の当たり判定を集める。細い障害物はここで飾りにする
    /// </summary>
    public void Collect()
    {
        _blocks.Clear();

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int deadLayer = LayerMask.NameToLayer("Dead");

        //まとまりごとに当たり判定をまとめる
        Dictionary<Transform, List<Block>> units = new Dictionary<Transform, List<Block>>();

        foreach (Collider col in _chunk.GetComponentsInChildren<Collider>())
        {
            if (!col.enabled) continue;
            int layer = col.gameObject.layer;
            if (layer != obstacleLayer && layer != deadLayer) continue;
            if (col.GetComponentInParent<BossHazard>()) continue;

            Transform unit = FindUnit(col.transform);
            if (!unit) continue;

            Block block = new Block { Unit = unit, Collider = col };
            if (!GetLocalBounds(col, out block.Min, out block.Max)) continue;

            if (!units.TryGetValue(unit, out List<Block> list))
            {
                list = new List<Block>();
                units.Add(unit, list);
            }
            list.Add(block);
        }

        foreach (KeyValuePair<Transform, List<Block>> pair in units)
        {
            Vector3 min = pair.Value[0].Min;
            Vector3 max = pair.Value[0].Max;
            foreach (Block b in pair.Value)
            {
                min = Vector3.Min(min, b.Min);
                max = Vector3.Max(max, b.Max);
            }

            //細い物は当たり判定を消して飾りにする
            if (max.x - min.x < SmallSize && max.z - min.z < SmallSize)
            {
                foreach (Collider col in pair.Key.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
                continue;
            }

            _blocks.AddRange(pair.Value);
        }
    }

    /// <summary>
    /// 必ず通れる道を決めて、ふさいでいる障害物をどける
    /// </summary>
    public void ClearCorridor(float startX, float width, float maxDrift, float margin)
    {
        CorridorStartX = startX;
        CorridorWidth = width;

        float roadWidth = _chunk.ChunkWidth;
        float minX = margin + width * 0.5f;
        float maxX = roadWidth - margin - width * 0.5f;

        //道の終わりの候補を並べて、どかす量が一番少ないものを選ぶ
        float bestEnd = Mathf.Clamp(startX, minX, maxX);
        float bestCost = float.MaxValue;
        const int candidates = 9;
        for (int i = 0; i < candidates; i++)
        {
            float end = Mathf.Lerp(minX, maxX, i / (candidates - 1f));
            if (Mathf.Abs(end - startX) > maxDrift) continue;

            float cost = 0f;
            foreach (Block b in _blocks)
            {
                if (Blocks(b, startX, end)) cost += 1f + (b.Max.x - b.Min.x) * (b.Max.z - b.Min.z) * 0.01f;
            }
            //同じくらいなら少しだけ道を曲げる（毎回まっすぐだと単調）
            cost += Random.value * 0.5f;

            if (cost < bestCost)
            {
                bestCost = cost;
                bestEnd = end;
            }
        }
        CorridorEndX = bestEnd;

        //ふさいでいるまとまりを消す
        RemoveUnits(b => Blocks(b, CorridorStartX, CorridorEndX));
    }

    /// <summary>
    /// 清水 四角い範囲（ローカル座標）にかかる障害物をどけて、そこにはアイテムも置かない（ジャンプ台を置くときなど）
    /// </summary>
    public void ClearArea(Rect area)
    {
        RemoveUnits(b => b.Max.x > area.xMin && b.Min.x < area.xMax && b.Max.z > area.yMin && b.Min.z < area.yMax);
        _noItemAreas.Add(area);
    }

    //条件に当てはまる当たり判定のまとまりを消す
    private void RemoveUnits(System.Predicate<Block> match)
    {
        HashSet<Transform> remove = new HashSet<Transform>();
        foreach (Block b in _blocks)
        {
            if (match(b)) remove.Add(b.Unit);
        }

        //消したものの上に乗っていた物も一緒に消す（宙に浮かないように）
        foreach (Transform unit in new List<Transform>(remove))
        {
            RemoveStackedOn(unit, remove);
        }

        foreach (Transform unit in remove)
        {
            if (!unit) continue;

            //マップそのものに付いている当たり判定なら、マップごと消さずに当たり判定だけ切る
            if (IsMapRoot(unit))
            {
                foreach (Block b in _blocks)
                {
                    if (b.Unit == unit && b.Collider && match(b)) b.Collider.enabled = false;
                }
                continue;
            }

            Object.Destroy(unit.gameObject);
        }
        //消した物と、当たり判定を切った物はもう数えない
        _blocks.RemoveAll(b => (remove.Contains(b.Unit) && !IsMapRoot(b.Unit)) || (b.Collider && !b.Collider.enabled));
    }

    private bool IsMapRoot(Transform unit)
    {
        return unit.parent == _chunk.transform && unit.childCount > 0;
    }

    /// <summary>
    /// 道の真ん中のx（ローカル座標）
    /// </summary>
    public float CorridorX(float localZ)
    {
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localZ / _chunk.ChunkLength));
        return Mathf.Lerp(CorridorStartX, CorridorEndX, t);
    }

    /// <summary>
    /// この場所に障害物が無いか（アイテムを置ける場所か）
    /// </summary>
    public bool IsFree(float localX, float localZ, float radius)
    {
        foreach (Rect area in _noItemAreas)
        {
            if (area.Contains(new Vector2(localX, localZ))) return false;
        }

        float ground = _chunk.GetLocalHeight(localX, localZ);
        foreach (Block b in _blocks)
        {
            if (!b.Unit) continue;
            if (localX + radius < b.Min.x || localX - radius > b.Max.x) continue;
            if (localZ + radius < b.Min.z || localZ - radius > b.Max.z) continue;
            if (b.Max.y < ground - 0.5f || b.Min.y > ground + RunHeight) continue;
            return false;
        }
        return true;
    }

    //この当たり判定が道をふさいでいるか
    private bool Blocks(Block b, float startX, float endX)
    {
        if (!b.Unit) return false;

        //地面から走る高さまでにかかっていなければ、下をくぐれる・上を飛び越えられる
        float ground = _chunk.GetLocalHeight((b.Min.x + b.Max.x) * 0.5f, (b.Min.z + b.Max.z) * 0.5f);
        if (b.Max.y < ground - 0.5f || b.Min.y > ground + RunHeight) return false;

        //その奥行きの間で道が通るxの範囲
        float zMin = Mathf.Clamp(b.Min.z, 0f, _chunk.ChunkLength);
        float zMax = Mathf.Clamp(b.Max.z, 0f, _chunk.ChunkLength);
        if (b.Max.z < 0f || b.Min.z > _chunk.ChunkLength) return false;

        float c0 = Mathf.Lerp(startX, endX, Mathf.SmoothStep(0f, 1f, zMin / _chunk.ChunkLength));
        float c1 = Mathf.Lerp(startX, endX, Mathf.SmoothStep(0f, 1f, zMax / _chunk.ChunkLength));
        float half = CorridorWidth * 0.5f;
        float left = Mathf.Min(c0, c1) - half;
        float right = Mathf.Max(c0, c1) + half;

        return b.Max.x > left && b.Min.x < right;
    }

    private void RemoveStackedOn(Transform unit, HashSet<Transform> remove)
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Block b in _blocks)
        {
            if (b.Unit != unit) continue;
            min = Vector3.Min(min, b.Min);
            max = Vector3.Max(max, b.Max);
        }
        if (min.x > max.x) return;

        Transform parent = unit.parent;
        if (!parent || parent == _chunk.transform) return;

        foreach (Transform other in parent)
        {
            if (other == unit || remove.Contains(other)) continue;

            //半分より上にある物だけ「上に乗っている」とみなす（高架の下の木などは残す）
            Vector3 local = _chunk.transform.InverseTransformPoint(other.position);
            float middle = (min.y + max.y) * 0.5f;
            bool onTop = local.x >= min.x && local.x <= max.x && local.z >= min.z && local.z <= max.z && local.y > middle;
            if (onTop) remove.Add(other);
        }
    }

    //当たり判定が属する「障害物1つ」を探す（地面 → マップ → 障害物 の、障害物の階層）
    private Transform FindUnit(Transform t)
    {
        Transform chunk = _chunk.transform;
        if (t == chunk) return null;

        Transform top = t;
        while (top.parent && top.parent != chunk) top = top.parent;
        if (top.parent != chunk) return null;

        //壁はさわらない
        if (top.name == "LeftWall" || top.name == "RightWall") return null;

        //マップ（子が並んでいる）なら、その子が障害物1つ
        if (t == top || top.childCount == 0) return top;

        Transform unit = t;
        while (unit.parent != top) unit = unit.parent;
        return unit;
    }

    //当たり判定の範囲を地面1枚のローカル座標で求める
    private bool GetLocalBounds(Collider col, out Vector3 min, out Vector3 max)
    {
        min = Vector3.positiveInfinity;
        max = Vector3.negativeInfinity;

        if (col is BoxCollider box)
        {
            Vector3 half = box.size * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = box.center + new Vector3(
                    (i & 1) == 0 ? -half.x : half.x,
                    (i & 2) == 0 ? -half.y : half.y,
                    (i & 4) == 0 ? -half.z : half.z);
                Vector3 local = _chunk.transform.InverseTransformPoint(box.transform.TransformPoint(corner));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            return true;
        }

        //箱以外は見た目の大きさで代わりにする
        Renderer r = col.GetComponentInChildren<Renderer>();
        if (!r) return false;
        Bounds bounds = r.bounds;
        min = _chunk.transform.InverseTransformPoint(bounds.min);
        max = _chunk.transform.InverseTransformPoint(bounds.max);
        return true;
    }
}
