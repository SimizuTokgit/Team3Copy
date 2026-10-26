using UnityEngine;

public class CineCameraController : MonoBehaviour
{
    [SerializeField] private GameObject[] CineCamera;
    [SerializeField] private MotherComputer MotherComputer;

    private void Start()
    {
        MotherComputer = FindAnyObjectByType<MotherComputer>();
        if (MotherComputer) SetTarget(MotherComputer.transform);
    }

    //清水 ボスが何体も出るので、出てきたボスに合わせてカメラを置き直せるようにした
    public void SetTarget(Transform boss)
    {
        // 渡されたボスの名前を取得
        string bossName = boss.name.Replace("(Clone)", "");
        if (bossName == "MotherComputer")
        {
            CineCamera[0].transform.position = new Vector3(-45f, 45f, boss.position.z - 150f);
            CineCamera[1].transform.position = new Vector3(45f, 45f, boss.position.z - 150f);
            CineCamera[2].transform.position = new Vector3(20f, 45f, boss.position.z - 200f);
            CineCamera[3].transform.position = new Vector3(20f, 45f, boss.position.z - 400f);
        }
        else if(bossName == "MotherComputerMini")
        {
            CineCamera[0].transform.position = new Vector3(20f, 20f, boss.position.z - 10f);
            CineCamera[1].transform.position = new Vector3(26f, 20f, boss.position.z - 10f);
            CineCamera[2].transform.position = new Vector3(23f, 22f, boss.position.z - 20f);
            CineCamera[3].transform.position = new Vector3(23f, 22f, boss.position.z - 40f);
        }
    }
}
