using UnityEngine;
using UnityEngine.Windows;

public class AirDiveState : ICharacterState
{
    public void Enter(Player player)
    {
        //player.Animation.SetTrigger("Dive");
        player.MoveController.SetIsDive(true);

        player.PlayerCamera.SetTargetFov(75);
    }
    public void Execute(Player player, InputInfo inputInfo)
    {

        //前進処理
        Vector3 nextVelocity = player.MoveController.AirMoveForward(player.Physics.GetVelocity(), player.transform.forward);

        player.transform.rotation = Quaternion.Lerp(player.transform.rotation, 
            Quaternion.Euler(30, player.transform.localEulerAngles.y, player.transform.localEulerAngles.z),40 * Time.deltaTime);

        //回転処理
        player.MoveController.Turn(inputInfo, player.transform, player.PlayerStatus, null);

        //速度適応
        player.Physics.SetVelocity(nextVelocity);

        player.GroundChecker.MoveAlongGround(player.GroundChecker.GetDownNormalVector(), 10);

        // 着地判定と状態遷移
        if (player.GroundChecker.IsGrounded())
        {
            //var velocty = player.MoveController.LandingBoost(player.Physics.GetVelocity(), player.transform, player.GroundChecker.OnGroundBoost(), player.PlayerStatus);
            //player.Physics.SetVelocity(velocty);
            //player.Transition(this, new LandingState());

            if (player.GroundChecker.OnGroundBoost() == PlayerGroundChecker.LandingRank.Perfect)
            {
                player.Transition(this, new BoostLandingState());
            }
            else
            {
                player.Transition(this, new LandingState());
            }
        }

        if(!inputInfo.IsDiveHeld)
        {
            player.Transition(this, new AirMoveState());
        }

    }

    public void Exit(Player player)
    {
        player.MoveController.SetIsDive(false);
        player.Animation.ResetTrigger("Dive");

        player.PlayerCamera.FovReSet();
    }
}

