using System.Collections.Generic;
using UnityEngine;

public class GenerateObstacle : MonoBehaviour
{
    //本庄
    //障害物の生成

    //障害物を入れる
    [SerializeField]
    private GameObject[] obstacles;

    //つなげたい最初の障害物
    [SerializeField]
    private GameObject ConnectStartobstacle;

    //つなげる障害物
    [SerializeField]
    private GameObject[] Connectobstacle;

    [SerializeField]
    private GameObject[] TentaclesObstacles;

    [SerializeField]
    private GameObject TentaclesConnectStartobstacle;

    [SerializeField]
    private GameObject[] TentaclesConnectobstacle;

    //次からつなげるかどうか
    public bool Connect;

    //つなげる回数
    public int ContinueObstacleNumber;

    //清水 障害物を見やすくするアウトライン（QuickOutline）
    [SerializeField] private bool UseOutline = true;
    [SerializeField] private Color ObstacleOutlineColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private float ObstacleOutlineWidth = 4f;

    private GroundAutomatically _groundMesh;
    private readonly List<GameObject> _placed = new List<GameObject>();

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
    }
    public Difficulty difficulty;

    public void DeleteAllObstacles()
    {
        // 削除対象のプレハブをまとめる
        List<GameObject> prefabs = new List<GameObject>();

        prefabs.AddRange(obstacles);

        if (ConnectStartobstacle != null)
            prefabs.Add(ConnectStartobstacle);

        prefabs.AddRange(Connectobstacle);

        // シーン内の全GameObjectを取得
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in sceneObjects)
        {
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) continue;

                // Instantiateされたオブジェクトは通常 "(Clone)" が付く
                if (obj.name == prefab.name || obj.name == prefab.name + "(Clone)")
                {
                    Destroy(obj);
                    break;
                }
            }
        }
    }

    public void Generate(Mesh mesh, int width, int length, Difficulty difficulty, float posY)
    {
        if (obstacles == null || obstacles.Length == 0) return;

        int spawnCount = 0;

        switch (difficulty)
        {
            case Difficulty.Easy:
                spawnCount = 1;
                break;

            case Difficulty.Normal:
                spawnCount = 1;
                break;

            case Difficulty.Hard:
                spawnCount = 1;
                break;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            int x = width / 2;
            int z = length / 2;

            int index = x + z * (width + 1);

            if (index >= mesh.vertices.Length) continue;

            Vector3 pos = mesh.vertices[index];

            if (!Connect)
            {
                int objNumber = Random.Range(0, obstacles.Length);

                if (obstacles[objNumber] == ConnectStartobstacle)
                    Connect = true;

                GameObject obj = Instantiate(obstacles[objNumber], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);
            }
            else
            {
                GameObject obj = Instantiate(Connectobstacle[Random.Range(0, Connectobstacle.Length)], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);

                if (ContinueObstacleNumber == 0)
                {
                    Connect = false;
                }
            }
        }
    }

    public void TentaclesGenerate(Mesh mesh, int width, int length, int spawnCount, float posY)
    {
        if (TentaclesObstacles == null || TentaclesObstacles.Length == 0) return;

        for (int i = 0; i < spawnCount; i++)
        {
            int x = width / 2;
            int z = length / 2;

            int index = x + z * (width + 1);

            if (index >= mesh.vertices.Length) continue;

            Vector3 pos = mesh.vertices[index];

            if (!Connect)
            {
                int objNumber = Random.Range(0, TentaclesObstacles.Length);

                if (TentaclesObstacles[objNumber] == TentaclesConnectStartobstacle)
                    Connect = true;

                GameObject obj = Instantiate(TentaclesObstacles[objNumber], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);
                ApplyOutlines();
            }
            else
            {
                GameObject obj = Instantiate(TentaclesConnectobstacle[Random.Range(0, TentaclesConnectobstacle.Length)], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);
                Place(obj);
                ApplyOutlines();

                if (ContinueObstacleNumber == 0)
                {
                    Connect = false;
                }
            }
        }
    }

    /// <summary>
    /// 清水 地面に合わせて置き直す。アウトラインは ApplyOutlines でまとめて付ける
    /// </summary>
    private void Place(GameObject obj)
    {
        if (!_groundMesh) _groundMesh = GetComponent<GroundAutomatically>();
        if (_groundMesh) _groundMesh.SnapToGround(obj.transform);

        _placed.Add(obj);
    }

    /// <summary>
    /// 清水 置いた障害物のうち、当たり判定が残っている物だけにアウトラインを付ける
    /// （細くて飾りになった物には付けない）
    /// </summary>
    public void ApplyOutlines()
    {
        foreach (GameObject obj in _placed)
        {
            if (!obj || !UseOutline) continue;

            if (obj.transform.childCount == 0)
            {
                if (HasEnabledCollider(obj)) AddOutline(obj, ObstacleOutlineColor, ObstacleOutlineWidth);
                continue;
            }

            foreach (Transform child in obj.transform)
            {
                if (HasEnabledCollider(child.gameObject)) AddOutline(child.gameObject, ObstacleOutlineColor, ObstacleOutlineWidth);
            }
        }
        _placed.Clear();
    }

    private static bool HasEnabledCollider(GameObject obj)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
        {
            if (col.enabled) return true;
        }
        return false;
    }

    public static void AddOutline(GameObject obj, Color color, float width)
    {
        Outline outline = obj.GetComponent<Outline>();
        if (!outline) outline = obj.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = color;
        outline.OutlineWidth = width;
    }

    public bool Connectobj()
    {
        return Connect;
    }

    public void Connectend()
    {
        Connect = false;
    }
    public void ConnectStart(int Number)
    {
        Connect = true;

        ContinueObstacleNumber = Number;
    }
}
