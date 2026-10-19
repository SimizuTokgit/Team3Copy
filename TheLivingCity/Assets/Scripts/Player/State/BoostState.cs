using UnityEngine;

public class BoostState : ICharacterState
{
    private readonly float boostTime = 1.5f;
    private float time = 0.0f;
    public void Enter(Player player)
    {
        player.Animation.SetTrigger("Iron");
        var velocty = player.MoveController.LandingBoost(player.Physics.GetVelocity(), player.transform, PlayerGroundChecker.LandingRank.Perfect, player.PlayerStatus, player.tutorialManager);
        player.Physics.SetVelocity(velocty);
        player.PlayerCamera.SetTargetFov(80);

        //清水 ブーストしている間はずっと無敵
        player.SetInvincibleTime(boostTime);
        player.EffectManager.PlayBoostEffect();
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

        //ブースト中はジャンプできないようにしてみる
        //if (!player.GroundChecker.IsGrounded() && !player.GroundChecker.IsDownhill())
        //{
        //    player.Physics.SetVelocity(player.MoveController.Jump(player.Physics.GetVelocity(), player.transform));
        //    player.Transition(this, new AirMoveState());
        //}

        time += Time.deltaTime;

        if (time >= boostTime)
        {
            player.Transition(this, new GroundMoveState());
        }

        player.Animation.SetFloat("Speed", player.Physics.GetVelocity().magnitude);
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("Iron");
    }
}

