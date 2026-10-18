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
            }
            else
            {
                GameObject obj = Instantiate(Connectobstacle[Random.Range(0, Connectobstacle.Length)], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);

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
            }
            else
            {
                GameObject obj = Instantiate(TentaclesConnectobstacle[Random.Range(0, Connectobstacle.Length)], transform);

                obj.transform.localPosition = new Vector3(pos.x, 0, pos.z);

                if (ContinueObstacleNumber == 0)
                {
                    Connect = false;
                }
            }
        }
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
