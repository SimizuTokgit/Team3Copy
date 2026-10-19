using UnityEngine;

//柴田
//Upgradeしたステータスを保存するクラス
//ステージをまたぐクラス

public class PlayerUpGradeStatusHolder :SingletonMonoBehaviour<PlayerUpGradeStatusHolder>
{
    //ステージをまたぐときにステータスを一時的に保存しておくための変数
    private PlayerStatus playerStatus;

    //ステージをまたぐときにステータスを一時的に保存しておくための関数
    //ゴールした時に呼び出す
    public void SaveUpGradeStatus(PlayerStatus nowStatus)
    {
        playerStatus = nowStatus;
    }

    public PlayerStatus LoadUpGradeStatus()
    {
        return playerStatus;
    }
}
