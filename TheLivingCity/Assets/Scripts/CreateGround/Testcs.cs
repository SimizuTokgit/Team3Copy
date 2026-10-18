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

    private void Start()
    {
        PassageNumber = -1;
        NextCheckPointNumber = 0;

        checkPointObject = FindAnyObjectByType<CheckPointInstance>();
        generetoGround = FindAnyObjectByType<GeneretoGround>();
        itemUI = FindAnyObjectByType<ItemUI>();
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
            PassageNumber++;
            NextCheckPointNumber++;
        }
    }
}
