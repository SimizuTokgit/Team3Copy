using UnityEngine;

//安井
//ゴールの処理
public class Goal : MonoBehaviour
{
    private GameManager GameManager;
    private GeneretoGround GeneretoGround;

    [SerializeField] private GameObject Boss;
    [SerializeField] private GameObject CutSceneDirectorObject;

    [SerializeField] private float POS_Z = 2000f;

    private void Awake()
    {
        //清水 エンドレスにはゴールもボスも無い
        //ストーリーで BossBattleManager がいるときは、そちらがボス戦を進めるのでこのゴールは使わない
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if ((gameManager && gameManager.IsEndless) || FindAnyObjectByType<BossBattleManager>())
        {
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        GameManager = FindAnyObjectByType<GameManager>();
        GeneretoGround = FindAnyObjectByType<GeneretoGround>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<Player>())
        {
            GeneretoGround._isBoss = true;
            GeneretoGround.meshInfo._generateObstacle.DeleteAllObstacles();
            GeneretoGround.meshInfo._itemGenerate.DeleteAllItems();
            Instantiate(Boss, new Vector3(24, 20, transform.position.z + POS_Z), Boss.gameObject.transform.rotation);
            CutSceneDirectorObject.SetActive(true);
            //GameManager._currentState = GameManager.GameState.GameClear;
        }
    }
}
