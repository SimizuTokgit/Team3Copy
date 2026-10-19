using UnityEngine;

public class AirDamageState : ICharacterState
{
    public void Enter(Player player)
    {
        player.Animation.SetTrigger("AirDamage");
        player.PlayerCamera.FovReSet();

    }

    public void Execute(Player player, InputInfo inputInfo)
    {
        //前進処理
        Vector3 nextVelocity = player.MoveController.AirMoveForward(player.Physics.GetVelocity(), player.transform.forward);

        //回転処理
        player.MoveController.Turn(inputInfo, player.transform, player.PlayerStatus, null);

        //速度適応
        player.Physics.SetVelocity(nextVelocity);

        player.GroundChecker.MoveAlongGround(player.GroundChecker.GetDownNormalVector(), 10);
        // 着地判定と状態遷移
        if (player.GroundChecker.IsGrounded())
        {
            player.Transition(this, new GroundMoveState());
        }

        else if (player.Animation.AnimationEnd("AirDamage"))
        {
            player.Transition(this, new AirMoveState());
        }
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("AirDamage");

    }
}
