using UnityEngine;

//清水
//ジャンプ台から飛び出したプレイヤーを、ボスに届く高さで飛ばす
//前に進む速さはそのままで、上向きの速さだけ計算して入れる
public class BossRampLauncher : MonoBehaviour
{
    private const float Gravity = 12f;        //AirMoveState の重力（PlayerMoveController.NormalGravity）
    private const float AimHeight = 8f;       //ボスの中心より少し上をねらう
    private const float MaxWaitTime = 0.5f;   //空中の状態になるまで待つ時間

    private Transform _target;
    private Player _pending;
    private float _pendingTime;
    private bool _launched = false;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_launched) return;

        //吸い寄せ用の子のコライダーではなく、本体だけ
        Player player = other.GetComponent<Player>();
        if (!player) return;

        _pending = player;
        _pendingTime = 0f;
    }

    //プレイヤーの状態の処理（地上→空中で上向きの速さが決まる）の後に上書きする
    private void LateUpdate()
    {
        if (!_pending || _launched) return;

        _pendingTime += Time.deltaTime;

        ICharacterState state = _pending.StateManager.CurrentState;
        bool inAir = state is AirMoveState || state is AirDiveState;
        if (!inAir)
        {
            if (_pendingTime > MaxWaitTime) _pending = null;
            return;
        }

        Launch(_pending);
        _launched = true;
        _pending = null;
    }

    private void Launch(Player player)
    {
        Vector3 velocity = player.Physics.GetVelocity();
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        float speed = Mathf.Max(flat.magnitude, 25f);

        Vector3 aim = _target ? _target.position + Vector3.up * AimHeight : player.transform.position + player.transform.forward * 60f;
        float distance = Mathf.Max(aim.z - player.transform.position.z, 10f);
        float time = distance / speed;
        float dy = aim.y - player.transform.position.y;

        //time 秒後に高さ dy に届く上向きの速さ
        float vy = (dy + 0.5f * Gravity * time * time) / time;
        vy = Mathf.Clamp(vy, 6f, 45f);

        player.Physics.SetVelocity(flat.normalized * speed + Vector3.up * vy);

        //突撃の演出
        ScreenEffects.Get().Shake(0.6f, 0.4f);
        ScreenEffects.Get().SlowMotion(0.6f, 0.35f);
        GameHUD.Get().Popup("CHARGE!!", GameHUD.Gold);
    }
}
