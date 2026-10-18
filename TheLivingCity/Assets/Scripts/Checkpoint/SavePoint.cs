using UnityEngine;

public class SavePoint : MonoBehaviour
{
    //本庄
    //チェックポイント

    //何番目のチェックポイントか　0からスタート
    public int ChackPointNumber;

    //このポイントを通過したか
    public bool Passed;

    public CheckPointObj check;

    private void Start()
    {
        Passed = false;
    }

    public void PassCheck()
    {
        Passed = true;
        check.GoMove();
    }
}
