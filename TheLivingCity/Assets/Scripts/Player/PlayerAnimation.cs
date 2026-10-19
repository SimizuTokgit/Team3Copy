using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator Animator { get; private set; }

    public bool _isAttack = true;
    public bool _isDamage = true;
    public bool _isChenge = true;
    public bool _isEvasion = true;

    private string _nowStateName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Animator = GetComponent<Animator>();
    }

    // Update is called once per frame
  
    public void SetTrigger(string name)
    {
        Animator.SetTrigger(name);
    }

    public void ResetTrigger(string name)
    {
        Animator.ResetTrigger(name);
    }

    public void SetFloat(string name, float value)
    {
        Animator.SetFloat(name, value);
    }

    public bool AnimationEnd(string name)
    {
        if(Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && Animator.GetCurrentAnimatorStateInfo(0).IsName(name))
        { return true; }

        return false;
    }

    public void SetAnimationSpeed(float speed)
    {
        Animator.speed = speed;
    }


    private void AttackEnd(string stateName)
    {
        if (_nowStateName != stateName) return;
        _isAttack = false;
    }

    //public void EvasionStart()
    //{
    //    _isEvasion=true;
    //    _isChenge = false;
    //    _isDamage = false;
    //    _isAttack = false;
    //}
    //private void EvasionEnd()
    //{
    //    _isEvasion = false;
    //}

    private void DamageEnd(string stateName)
    {
        if (_nowStateName != stateName) return;
        _isDamage = false;
    }

    private void ChengeStart()
    {
        _isChenge = true;
    }

    private void ChengeEnd()
    {
        _isChenge = false;
    }

    public void PlayRunSE()
    {
        SoundManager.Instance.PlaySE("Run");
    }
}
