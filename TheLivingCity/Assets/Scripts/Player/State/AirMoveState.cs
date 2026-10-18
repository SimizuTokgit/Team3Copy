using UnityEngine;
public class AirMoveState : ICharacterState
{
    private readonly float boostLandingAbleSpeed = 30;
    public void Enter(Player player)
    { 
        player.Animation.SetTrigger("Fly");
        SoundManager.Instance.PlaySE("Jump");

    }
    public void Execute(Player player, InputInfo inputInfo)
    {
        //前進処理
        Vector3 nextVelocity = player.MoveController.AirMoveForward(player.Physics.GetVelocity(), player.transform.forward);

        //回転処理
        player.MoveController.Turn(inputInfo, player.transform,player.PlayerStatus, null);

        //速度適応
        player.Physics.SetVelocity(nextVelocity);

        player.GroundChecker.MoveAlongGround(player.GroundChecker.GetDownNormalVector(),10);

        // 着地判定と状態遷移
        if (player.GroundChecker.IsGrounded())
        {

            if (player.GroundChecker.OnGroundBoost() == PlayerGroundChecker.LandingRank.Perfect && player.Physics.GetVelocity().magnitude >= boostLandingAbleSpeed)
            {
                player.Transition(this, new BoostLandingState());
            }
            else
            {
                player.Transition(this, new LandingState());
            }

        }

        if(inputInfo.IsDiveHeld)
        {
            player.Transition(this, new AirDiveState());
        }
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("Fly");
    }
}