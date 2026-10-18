using UnityEngine;

public class CineCameraController : MonoBehaviour
{
    [SerializeField] private GameObject[] CineCamera;
    [SerializeField] private MotherComputer MotherComputer;

    private void Start()
    {
        MotherComputer = FindAnyObjectByType<MotherComputer>();
        CineCamera[0].transform.position = new Vector3(-45f, 45f, MotherComputer.transform.position.z - 150f);
        CineCamera[1].transform.position = new Vector3(45f, 45f, MotherComputer.transform.position.z - 150f);
        CineCamera[2].transform.position = new Vector3(20f, 45f, MotherComputer.transform.position.z - 200f);
        CineCamera[3].transform.position = new Vector3(20f, 45f, MotherComputer.transform.position.z - 400f);
    }
}
