using UnityEngine;

//清水
//Highway を傾けて坂にしたジャンプ台
//ボスの手前に置くときは、坂を登りきって飛び出すと BossRampLauncher がボスに届くように飛ばしてくれる
//道の途中にたまに置くとき（target なし）は、ふつうに飛び出すだけ
public class BossRamp : MonoBehaviour
{
    private Transform _player;
    private float _endZ;

    /// <summary>
    /// ジャンプ台を作る。startPos は坂の登り口（地面の上）
    /// </summary>
    public static BossRamp Create(GameObject highwayPrefab, Vector3 startPos, float angle, Transform target, Color outlineColor)
    {
        GameObject root = new GameObject("BossRamp");
        root.transform.position = startPos;
        BossRamp ramp = root.AddComponent<BossRamp>();

        //見た目：Highway の当たり判定を使って、道路面の大きさと位置を調べる
        GameObject visual = Instantiate(highwayPrefab, root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        BoxCollider deck = FindDeck(visual);
        float width = 18f;
        float length = 50f;
        Vector3 nearTop = startPos;
        if (deck)
        {
            Vector3 c = deck.center;
            Vector3 h = deck.size * 0.5f;
            nearTop = deck.transform.TransformPoint(new Vector3(c.x, c.y + h.y, c.z - h.z));
            Vector3 farTop = deck.transform.TransformPoint(new Vector3(c.x, c.y + h.y, c.z + h.z));
            length = Vector3.Distance(nearTop, farTop);
            width = Vector3.Distance(deck.transform.TransformPoint(new Vector3(c.x - h.x, c.y, c.z)), deck.transform.TransformPoint(new Vector3(c.x + h.x, c.y, c.z)));
        }

        //道路面の手前の端を軸に傾けて、手前の端を登り口に合わせる（橋脚は地面に埋まる）
        visual.transform.RotateAround(nearTop, Vector3.right, -angle);
        visual.transform.position += startPos + Vector3.up * 0.1f - nearTop;

        //Highway の当たり判定（障害物）は消して、走れる床を別に作る
        foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
        {
            Destroy(col);
        }
        foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = 0;
        }
        GenerateObstacle.AddOutline(visual, outlineColor, 5f);

        Quaternion tilt = Quaternion.Euler(-angle, 0f, 0f);

        //走れる床（Ground レイヤーなので、プレイヤーが坂に沿って登る）
        GameObject surface = new GameObject("RampSurface");
        surface.transform.SetParent(root.transform, false);
        surface.transform.localRotation = tilt;
        surface.layer = LayerMask.NameToLayer("Ground");
        BoxCollider floor = surface.AddComponent<BoxCollider>();
        floor.size = new Vector3(width, 0.5f, length);
        floor.center = new Vector3(0f, -0.15f, length * 0.5f);

        //坂の先の空中に、飛び出したらボスへ飛ばす範囲を置く
        Vector3 farEnd = startPos + tilt * new Vector3(0f, 0f, length);
        if (target)
        {
            GameObject launch = new GameObject("RampLaunch");
            launch.transform.SetParent(root.transform, false);
            launch.transform.position = farEnd + new Vector3(0f, 3f, 6f);
            BoxCollider zone = launch.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(width + 6f, 10f, 10f);
            launch.AddComponent<BossRampLauncher>().SetTarget(target);
        }

        ramp._endZ = farEnd.z;
        return ramp;
    }

    private void Start()
    {
        Player player = FindAnyObjectByType<Player>();
        if (player) _player = player.transform;
    }

    private void Update()
    {
        //地面の子になっているときは、地面を作り直すときに一緒に消える
        if (transform.parent) return;

        //通り過ぎたら片付ける
        if (_player && _player.position.z > _endZ + 150f) Destroy(gameObject);
    }

    //いちばん広い箱を道路面とみなす
    private static BoxCollider FindDeck(GameObject visual)
    {
        BoxCollider best = null;
        float bestArea = 0f;
        foreach (BoxCollider box in visual.GetComponentsInChildren<BoxCollider>(true))
        {
            Vector3 s = Vector3.Scale(box.size, box.transform.lossyScale);
            float area = Mathf.Abs(s.x * s.z);
            if (area > bestArea)
            {
                bestArea = area;
                best = box;
            }
        }
        return best;
    }
}
