using UnityEngine;
using UnityEngine.InputSystem.HID;

public class GroundMoveState : ICharacterState
{
    private readonly float jumpAbleSpeed = 30;

    public void Enter(Player player)
    {
        player.Animation.SetTrigger("Run");
        player.PlayerCamera.FovReSet();
    }

    public void Execute(Player player, InputInfo inputInfo)
    {
        //if (player.GameManager._currentState == GameManager.GameState.Ready)
        //    return;

        //加速処理
        player.MoveController.GroundAccel(player.PlayerStatus);

        //前進処理
        Vector3 nextVelocity = player.MoveController.GroundMoveForward(player.transform.forward);

        //回転処理
        player.MoveController.Turn(inputInfo, player.transform, player.PlayerStatus, player.tutorialManager);
        //速度適応
        player.Physics.SetVelocity(nextVelocity);

        if(player.GroundChecker.IsDownhill())
        {
            //下りの場合は真下の地面の法線ベクトルに沿って走る
            player.GroundChecker.MoveAlongGround(player.GroundChecker.GetDownNormalVector(), 33);
        }
        else
        {
            //上りの場合は少し前の地面の法線ベクトルに沿って走る
            player.GroundChecker.MoveAlongGround(player.GroundChecker.GetForwardNormalVector(), 20);
        }

        // 着地判定と状態遷移
        if (!player.GroundChecker.IsGrounded())
        {
            if (player.Physics.GetVelocity().magnitude >= jumpAbleSpeed)
            {
                player.Physics.SetVelocity(player.MoveController.Jump(player.Physics.GetVelocity(), player.transform));
                player.Transition(this, new AirMoveState());
            }

            else player.Transition(this, new AirMoveState());

            //else player.GroundChecker.MoveAlongGround(player.GroundChecker.GetDownNormalVector(), 33);
        }

        player.Animation.SetFloat("Speed", player.Physics.GetVelocity().magnitude);
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("Run");

    }
}
