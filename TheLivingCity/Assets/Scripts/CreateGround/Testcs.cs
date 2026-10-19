using UnityEngine;
public class Testcs : MonoBehaviour
{
    //何番目のチェックポイントを通ったか
    public int PassageNumber;

    //次通過予定のチェックポイントの番号
    public int NextCheckPointNumber;

    public CheckPointInstance checkPointObject;
    private ItemUI itemUI;


    public GeneretoGround generetoGround;

    //清水 チェックポイント通過で残り時間と体力を回復する
    private TimeManager timeManager;
    private Player player;
    [SerializeField] private int HealAmount = 30;

    private void Start()
    {
        PassageNumber = -1;
        NextCheckPointNumber = 0;

        checkPointObject = FindAnyObjectByType<CheckPointInstance>();
        generetoGround = FindAnyObjectByType<GeneretoGround>();
        itemUI = FindAnyObjectByType<ItemUI>();
        timeManager = FindAnyObjectByType<TimeManager>();
        player = GetComponent<Player>();
    }

    //清水 チェックポイント通過のごほうびと表示
    private void PassCheckPoint()
    {
        string message = "CHECKPOINT!";

        if (timeManager && timeManager.RecoveryTime > 0f)
        {
            timeManager.RecoverTime();
            message += $"\n<size=55%>TIME +{timeManager.RecoveryTime:0}";
        }

        int healed = player ? player.Heal(HealAmount) : 0;
        if (healed > 0)
        {
            message += message.Contains("<size") ? $"   HP +{healed}" : $"\n<size=55%>HP +{healed}";
        }

        GameHUD.Get().Banner(message, GameHUD.Good, 1.8f);
        GameHUD.Get().Flash(new Color(0.4f, 1f, 0.5f, 0.25f), 0.4f);
    }

    void Update()
    {
        if (NextCheckPointNumber >= checkPointObject.CheckPoints.Count)
            return;

        if (checkPointObject.CheckPoints[NextCheckPointNumber].transform.position.z <= transform.position.z)
        {
            generetoGround.Branches = true;
            checkPointObject.save.PassCheck();
            itemUI.ItemNumberCheck();
            generetoGround.PlayerPosGet();
            generetoGround.BranchStartSet();
            PassCheckPoint();
            PassageNumber++;
            NextCheckPointNumber++;
        }
    }
}
