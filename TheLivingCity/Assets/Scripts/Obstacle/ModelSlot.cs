using UnityEngine;

//清水
//見た目（モデル）を付け替えるための部品
//・プレハブに付けて Model に新しいモデルを入れると、元の見た目を隠して新しいモデルを出す
//・当たり判定はプレハブ側のものをそのまま使う（新しいモデルの当たり判定は外す）
//・当たり判定が無いモデルには、見た目の大きさから箱の当たり判定を自動で付けられる（EnsureCollider）
//ボス・障害物・ギミックのどれにも使える
public class ModelSlot : MonoBehaviour
{
    [SerializeField] private GameObject Model;                //差し替えるモデル（空なら何もしない）
    [SerializeField] private Vector3 Offset = Vector3.zero;   //位置のずれ
    [SerializeField] private Vector3 Euler = Vector3.zero;    //向きのずれ
    [SerializeField] private float Scale = 1f;                //大きさ
    [SerializeField] private bool HideOriginal = true;        //元の見た目を隠すか

    private void Awake()
    {
        if (Model) Apply(gameObject, Model, Offset, Euler, Scale, HideOriginal);
    }

    /// <summary>
    /// target の見た目を model に付け替える。作ったモデルを返す
    /// </summary>
    public static GameObject Apply(GameObject target, GameObject model, Vector3 offset, Vector3 euler, float scale, bool hideOriginal = true)
    {
        if (hideOriginal)
        {
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        GameObject swapped = Instantiate(model, target.transform);
        swapped.name = "SwappedModel";
        swapped.transform.localPosition = offset;
        swapped.transform.localRotation = Quaternion.Euler(euler);
        swapped.transform.localScale = model.transform.localScale * scale;

        //当たり判定は元のプレハブのものを使う
        foreach (Collider col in swapped.GetComponentsInChildren<Collider>(true))
        {
            Destroy(col);
        }

        //レイヤーを合わせる（障害物・一撃死の判定がレイヤーで決まるので）
        SetLayer(swapped, target.layer);
        return swapped;
    }

    /// <summary>
    /// 当たり判定が1つも無ければ、見た目の大きさに合わせた箱の当たり判定を付ける
    /// 新しいモデルをそのまま障害物にしたいとき用
    /// </summary>
    public static void EnsureCollider(GameObject obj, bool isTrigger)
    {
        if (obj.GetComponentInChildren<Collider>()) return;

        bool found = false;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 local = obj.transform.InverseTransformPoint(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            found = true;
        }
        if (!found) return;

        BoxCollider box = obj.AddComponent<BoxCollider>();
        box.center = (min + max) * 0.5f;
        box.size = max - min;
        box.isTrigger = isTrigger;
    }

    public static void SetLayer(GameObject obj, int layer)
    {
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layer;
        }
    }
}
