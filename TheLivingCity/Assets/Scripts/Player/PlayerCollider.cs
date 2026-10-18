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
                _player.Damage();

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
