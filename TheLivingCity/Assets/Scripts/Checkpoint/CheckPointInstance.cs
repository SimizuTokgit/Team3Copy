using System.Collections.Generic;
using UnityEngine;

public class CheckPointInstance : MonoBehaviour
{
    //本庄
    //チェックポイント生成

    //チェックポイント
    [SerializeField]
    private GameObject CheckPoint;

    //チェックポイントの間隔
    [SerializeField]
    private float pointIntervals;

    //ゴールのオブジェクト
    private GameObject Goleobj;

    //チェックポイントに生成するオブジェクト
    [SerializeField]
    private GameObject CheckPointObj;
    private CheckPointObj checkobj;

    //ゴールのz座標
    private float golePosition;

    //この中にチェックポイントが入る
    public List<GameObject> CheckPoints = new List<GameObject>();

    //何個目のチェックポイントか
    private int checkpointNumber;

    //今までのチェックポイント座標のずれ
    private float checkpointOffset;

    public SavePoint save;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Goleobj = FindAnyObjectByType<Goal>().gameObject;
        golePosition = Goleobj.transform.position.z;

        checkpointNumber = 1;

        checkpointOffset = 0.0f;

        InstanceCheckPoints(0.0f);
    }

    /// <summary>
    ///チェックポイントの生成
    /// </summary>
    public void InstanceCheckPoints(float moveDis)
    {
        //次のチェックポイントがゴールを超えたら生成しない
        if (golePosition <= pointIntervals * checkpointNumber + moveDis + checkpointOffset)
            return;

        checkpointOffset += moveDis;

        GameObject Check = Instantiate(CheckPoint, new Vector3(0, 0, pointIntervals * checkpointNumber + moveDis + checkpointOffset), Quaternion.identity);

        GameObject checkObj = Instantiate(CheckPointObj, Check.transform.position + new Vector3(24, 20, 0), Quaternion.identity);

        checkobj = checkObj.GetComponent<CheckPointObj>();

        checkobj.CheckPoint = Check;

        checkobj.PlayerSet(FindAnyObjectByType<Player>().gameObject);

        save = Check.GetComponent<SavePoint>();
        save.check = checkobj;

        save.ChackPointNumber = checkpointNumber - 1;

        CheckPoints.Add(Check);

        checkpointNumber++;
    }
}
