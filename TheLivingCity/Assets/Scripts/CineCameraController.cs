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
        CineCamera[0].transform.position = new Vector3(-45f, 45f, boss.position.z - 150f);
        CineCamera[1].transform.position = new Vector3(45f, 45f, boss.position.z - 150f);
        CineCamera[2].transform.position = new Vector3(20f, 45f, boss.position.z - 200f);
        CineCamera[3].transform.position = new Vector3(20f, 45f, boss.position.z - 400f);
    }
}
