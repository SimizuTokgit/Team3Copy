using UnityEngine;
//柴田
//プレイヤーのコライダーが関係する部分のクラス
public class PlayerCollider : MonoBehaviour
{
    private Player _player;
    private DamageScreen _damageScreen;
    private void Start()
    {
        _player = GetComponent<Player>();
        _damageScreen = FindAnyObjectByType<DamageScreen>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            if (!_player.isInvincible)  //無敵かどうか
            {
                _damageScreen.Damage();

                //清水 障害物ごとの HP の減り（木・看板など細い物は小さい）
                ObstacleInfo info = other.GetComponentInParent<ObstacleInfo>();
                _player.Damage(info ? info.GetDamageRate() : 1f);

                if (_player.tutorialManager != null)
                {
                    if (_player.tutorialManager.tutorialCount == 3)
                    {
                        _player.tutorialManager.DodgeTimeReset();
                    }
                }

            }
        }

        //一撃死亡オブジェクトに当たった時の処理
        else if(other.gameObject.layer == LayerMask.NameToLayer("Dead"))
        {
            if (!_player.isInvincible)  //無敵かどうか
            {
                _damageScreen.Damage();
                //清水 何にやられたかで、ゲームオーバーの演出を変える
                GameOverCause.Set(GameOverCause.FromHazard(other));
                _player.DeadDamage();

                if (_player.tutorialManager != null)
                {
                    if (_player.tutorialManager.tutorialCount == 3)
                    {
                        _player.tutorialManager.DodgeTimeReset();
                    }
                }

            }
        }
    }
}
