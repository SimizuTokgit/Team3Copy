using UnityEngine;
using UnityEngine.InputSystem;

public class CheckPointObj : MonoBehaviour
{
    //プレイヤー
    private GameObject player;

    //チェックポイント
    public GameObject CheckPoint;

    //プレイヤーに向かうかどうか
    private bool Goplayer;

    //プレイヤーに向かうスピード
    [SerializeField]
    private float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Goplayer = false;
    }

    void Update()
    {
        MoveToPlayer();
    }

    public void MoveToPlayer()
    {
        if (Goplayer)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position, Time.deltaTime * speed);
        }
    }

    public void PlayerSet(GameObject Player)
    {
        player = Player;
    }

    public void GoMove()
    {
        Goplayer = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == player)
        {
            Destroy(gameObject);
        }
    }
}
