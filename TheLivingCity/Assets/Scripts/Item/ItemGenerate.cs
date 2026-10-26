using UnityEngine;

public class ItemGenerate : MonoBehaviour
{
    //本庄
    //アイテムの生成

    [System.Serializable]
    public class ItemData
    {
        //アイテム名
        public string itemName;

        //Prefab
        public GameObject prefab;

        //出現率
        public int weight;
    }

    //アイテム一覧
    [SerializeField]
    private ItemData[] items;

    //生成数
    public int GenerateCount;

    //浮き倍率
    [SerializeField]
    private float ItemUpRate;

    public void DeleteAllItems()
    {
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in sceneObjects)
        {
            foreach (ItemData item in items)
            {
                if (item == null || item.prefab == null) continue;

                if (obj.name == item.prefab.name || obj.name == item.prefab.name + "(Clone)")
                {
                    Destroy(obj);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// アイテム生成
    /// </summary>
    public void ItemGen(Mesh mesh, int width, int length)
    {
        for (int i = 0; i < GenerateCount; i++)
        {
            int itemNumber = GetWeightedRandom();

            int x = Random.Range(0, width);
            int z = Random.Range(0, length);

            int index = x + z * (width + 1);

            if (index >= mesh.vertices.Length)
                continue;

            Vector3 pos = mesh.vertices[index];

            GameObject item = Instantiate(items[itemNumber].prefab, transform);

            float itemup = Random.Range(1.0f, ItemUpRate);

            if (itemup <= 1.3f)
                itemup = 1.0f;

            item.transform.localPosition = new Vector3(pos.x, pos.y * itemup, pos.z);
        }
    }

    /// <summary>
    /// 清水 取れる場所にアイテムを置く
    /// 今までは高さが「地面の高さ×最大ItemUpRate倍」で、山の上だと10m以上浮いて取れなかった
    /// 障害物の中も避ける。一部は必ず通れる道の上に置く
    /// </summary>
    public void ItemGenReachable(GroundAutomatically chunk, ChunkLayout layout, float corridorRate, float airRate, float countRate = 1f)
    {
        const int tries = 10;          //置ける場所が見つかるまで試す回数
        const float edgeMargin = 3f;   //壁ぎわは避ける
        const float floatHeight = 1.2f; //地面からの高さ
        const float airMin = 4f;       //空中に置くときの高さ（ジャンプで届く）
        const float airMax = 8f;

        int count = Mathf.RoundToInt(GenerateCount * countRate);
        for (int i = 0; i < count; i++)
        {
            int itemNumber = GetWeightedRandom();
            bool onCorridor = Random.value < corridorRate;

            for (int t = 0; t < tries; t++)
            {
                float z = Random.Range(chunk.ChunkLength * 0.1f, chunk.ChunkLength * 0.9f);
                float x;
                if (onCorridor)
                {
                    float half = layout.CorridorWidth * 0.5f - 1f;
                    x = layout.CorridorX(z) + Random.Range(-half, half);
                }
                else
                {
                    x = Random.Range(edgeMargin, chunk.ChunkWidth - edgeMargin);
                }

                if (!layout.IsFree(x, z, 1.5f)) continue;

                //一部はジャンプで取る高さに浮かせる
                float height = Random.value < airRate ? Random.Range(airMin, airMax) : floatHeight;

                GameObject item = Instantiate(items[itemNumber].prefab, transform);
                item.transform.localPosition = new Vector3(x, chunk.GetLocalHeight(x, z) + height, z);
                break;
            }
        }
    }

    /// <summary>
    /// 清水 ジャンプ台の上の端（ローカル座標）から飛び出したときの軌道に沿ってアイテムを並べる
    /// だいたい秒速45mで飛び出したときの放物線（AirMoveState の重力 12）
    /// </summary>
    public void SpawnArc(Vector3 rampTop, int count)
    {
        const float speed = 45f;
        const float jumpSpeed = 2f + speed * 0.2f; //PlayerMoveController.Jump と同じ
        const float gravity = 12f;
        const float interval = 0.28f;              //アイテムの間隔（秒）

        for (int i = 1; i <= count; i++)
        {
            float t = interval * i;
            float up = jumpSpeed * t - 0.5f * gravity * t * t;

            GameObject item = Instantiate(items[GetWeightedRandom()].prefab, transform);
            item.transform.localPosition = rampTop + new Vector3(0f, up + 1f, speed * t);
        }
    }

    /// <summary>
    /// 清水 from から to（ローカル座標、高さは地面に合わせる）までまっすぐ count 個並べる
    /// プレス機の下や電車の通り道など、一撃死のギミックの近くに置く
    /// </summary>
    public void SpawnLine(GroundAutomatically chunk, Vector3 from, Vector3 to, int count, float height = 1.2f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 p = Vector3.Lerp(from, to, count == 1 ? 0.5f : (float)i / (count - 1));
            p.x = Mathf.Clamp(p.x, 2f, chunk.ChunkWidth - 2f);
            Spawn(chunk, p.x, p.z, height);
        }
    }

    /// <summary>
    /// 清水 center（ローカル座標）のまわりに半径 radius で count 個並べる（感電した物のまわりなど）
    /// </summary>
    public void SpawnRing(GroundAutomatically chunk, Vector3 center, float radius, int count, float height = 1.2f)
    {
        float start = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float a = (start + 360f * i / count) * Mathf.Deg2Rad;
            float x = Mathf.Clamp(center.x + Mathf.Sin(a) * radius, 2f, chunk.ChunkWidth - 2f);
            float z = Mathf.Clamp(center.z + Mathf.Cos(a) * radius, 1f, chunk.ChunkLength - 1f);
            Spawn(chunk, x, z, height);
        }
    }

    private void Spawn(GroundAutomatically chunk, float x, float z, float height)
    {
        if (items == null || items.Length == 0) return;
        GameObject item = Instantiate(items[GetWeightedRandom()].prefab, transform);
        item.transform.localPosition = new Vector3(x, chunk.GetLocalHeight(x, z) + height, z);
    }

    /// <summary>
    /// 重み付きランダム
    /// </summary>
    private int GetWeightedRandom()
    {
        int totalWeight = 0;

        for (int i = 0; i < items.Length; i++)
        {
            totalWeight += items[i].weight;
        }

        if (totalWeight <= 0)
            return 0;

        int randomValue = Random.Range(0, totalWeight);

        int currentWeight = 0;

        for (int i = 0; i < items.Length; i++)
        {
            currentWeight += items[i].weight;

            if (randomValue < currentWeight)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// 名前で出現率変更
    /// </summary>
    public void SetItemWeight(string itemName, int newWeight)
    {
        for (int i = 0; i < items.Length; i++)
        {
            //名前比較
            if (items[i].itemName == itemName)
            {
                //0未満防止
                if (newWeight < 0)
                    newWeight = 0;

                items[i].weight = newWeight;
                return;
            }
        }

        Debug.LogWarning(itemName + " が見つかりません");
    }

    /// <summary>
    /// 名前で出現率加算
    /// </summary>
    public void AddItemWeight(string itemName, int addWeight)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemName == itemName)
            {
                items[i].weight += addWeight;

                //0未満防止
                if (items[i].weight < 0)
                    items[i].weight = 0;

                return;
            }
        }

        Debug.LogWarning(itemName + " が見つかりません");
    }

    /// <summary>
    /// 名前で出現率取得
    /// </summary>
    public int GetItemWeight(string itemName)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemName == itemName)
            {
                return items[i].weight;
            }
        }

        Debug.LogWarning(itemName + " が見つかりません");

        return 0;
    }

    /// <summary>
    /// アイテム出現確立リセット
    /// </summary>
    public void ResetAllWeight()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i].weight = 1;
        }
    }
}
