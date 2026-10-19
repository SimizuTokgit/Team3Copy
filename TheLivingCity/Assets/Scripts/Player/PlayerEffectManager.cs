using UnityEngine;

public class PlayerEffectManager : MonoBehaviour
{
    [SerializeField] private GameObject JustLandingEffect;

    [SerializeField] private ParticleSystem BoostEffect;

    public void JustLanding()
    {
        var LandingEffect = Instantiate(JustLandingEffect,this.transform);
        Destroy(LandingEffect,1);
    }

    public void PlayBoostEffect()
    {
        BoostEffect.Play();
    }
}
