using UnityEngine;

public class LandingState : ICharacterState
{
    public void Enter(Player player)
    {
        player.Animation.SetTrigger("Land");
        player.PlayerCamera.FovReSet();

        SoundManager.Instance.PlaySE("NormalLanding");
    }
    public void Execute(Player player, InputInfo inputInfo)
    {
        Vector3 nextVelocity = player.MoveController.GroundMoveForward(player.transform.forward);

        //回転処理
        player.MoveController.Turn(inputInfo, player.transform, player.PlayerStatus, null);

        //速度適応
        player.Physics.SetVelocity(nextVelocity);

        if (player.GroundChecker.IsDownhill())
        {
            //下りの場合は真下の地面の法線ベクトルに沿って走る
            player.GroundChecker.MoveAlongGround(player.GroundChecker.GetDownNormalVector(), 33);
        }
        else
        {
            //上りの場合は少し前の地面の法線ベクトルに沿って走る
            player.GroundChecker.MoveAlongGround(player.GroundChecker.GetForwardNormalVector(), 20);
        }

        if (!player.GroundChecker.IsGrounded() && !player.GroundChecker.IsDownhill())
        {
            player.Physics.SetVelocity(player.MoveController.Jump(player.Physics.GetVelocity(), player.transform));
            player.Transition(this, new AirMoveState());
        }

        if (player.Animation.AnimationEnd("Land"))
        {
            player.Transition(this, new GroundMoveState());
        }
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("Land");

    }
}

