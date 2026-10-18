using UnityEngine;

public class BoostLandingState : ICharacterState
{
    public void Enter(Player player)
    {
        player.Animation.SetTrigger("DiveLand");
        player.Physics.SetVelocity(Vector3.zero);

        player.PlayerCamera.JustLandEffect();
        player.EffectManager.JustLanding();

        SoundManager.Instance.PlaySE("BoostLanding");
    }
    public void Execute(Player player, InputInfo inputInfo)
    { 

        if (player.Animation.AnimationEnd("DiveLand"))
        {
            player.Transition(this, new BoostState());
        }
    }

    public void Exit(Player player)
    {
        player.Animation.ResetTrigger("DiveLand");
        player.PlayerCamera.JustLandEffectOff();

    }
}

