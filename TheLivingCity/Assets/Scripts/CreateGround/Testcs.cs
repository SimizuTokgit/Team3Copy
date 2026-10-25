using System.Collections.Generic;
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
        //回復した量は見出しの下に数字画像で出す
        List<object> numbers = new List<object>();

        if (timeManager && timeManager.RecoveryTime > 0f)
        {
            timeManager.RecoverTime();
            numbers.Add("TIME +");
            numbers.Add(Mathf.RoundToInt(timeManager.RecoveryTime));
        }

        int healed = player ? player.Heal(HealAmount) : 0;
        if (healed > 0)
        {
            numbers.Add(numbers.Count > 0 ? "    HP +" : "HP +");
            numbers.Add(healed);
        }

        GameHUD.Get().Banner("CHECKPOINT!", GameHUD.Good, 1.8f, numbers.ToArray());
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
