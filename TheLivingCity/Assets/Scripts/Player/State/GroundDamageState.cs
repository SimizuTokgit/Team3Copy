using UnityEngine;

public class GroundDamageState : ICharacterState
{
    public void Enter(Player player)
    {
        player.Animation.SetTrigger("GroundDamage");
        player.PlayerCamera.FovReSet();

    }

    public void Execute(Player player, InputInfo inputInfo)
    {

        if (player.Animation.AnimationEnd("GroundDamage"))
        {
            player.Transition(this, new GroundMoveState());
        }
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("GroundDamage");

    }
}
