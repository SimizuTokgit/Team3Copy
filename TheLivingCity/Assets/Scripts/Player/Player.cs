using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public StateManager StateManager { get; private set; }
    public PlayerAnimation Animation { get; private set; }
    public PlayerMoveController MoveController { get; private set; }
    public PlayerCamera PlayerCamera { get; private set; }
    public GameManager GameManager { get; private set; }
    public PlayerPhysics Physics { get; private set; }
    public PlayerGroundChecker GroundChecker { get; private set; }
    public PlayerEffectManager EffectManager { get; private set; }
    public PlayerUIManager PlayerUIManager { get; private set; }
    public PlayerStatus PlayerStatus { get; private set; }

    private int hp = 100;
    private readonly int MAX_HP = 100;

    private readonly int GuaranteedDamage = 10;     //絶対受ける最低ダメージ

    //清水 いつまで無敵か。短い無敵を後から重ねても、長い無敵が途中で切れないようにした
    private float _invincibleUntil = 0f;
    public bool isInvincible => Time.time < _invincibleUntil;

    private Vector3 prevSpeed = Vector3.zero;

    //チュートリアル
    public TutorialManager tutorialManager;

    private void Awake()
    {
        //清水 画面効果と追加UIを用意しておく
        ScreenEffects.Get();
        GameHUD.Get();

        Animation = GetComponent<PlayerAnimation>();
        Physics = GetComponent<PlayerPhysics>();
        GroundChecker = GetComponent<PlayerGroundChecker>();
        PlayerCamera = FindAnyObjectByType<PlayerCamera>();
        EffectManager = GetComponent<PlayerEffectManager>();
        PlayerUIManager = GetComponent<PlayerUIManager>();
        PlayerUIManager.Init(hp);
        GameManager = FindAnyObjectByType<GameManager>();

        StateManager = new StateManager();
        MoveController = new PlayerMoveController();
        if(PlayerUpGradeStatusHolder.Instance.LoadUpGradeStatus() == null) PlayerStatus = new PlayerStatus();
        else PlayerStatus = PlayerUpGradeStatusHolder.Instance.LoadUpGradeStatus();

        MoveController.SetStartSpeed(PlayerStatus);
        Vector3 nextVelocity = MoveController.GroundMoveForward(transform.forward);
        Physics.InitVelocity(nextVelocity);
    }

    public void Execute(InputInfo inputInfo)
    {
        //清水 ボス登場の演出中（Event）も止める
        if (GameManager._currentState != GameManager.GameState.Pause &&
            GameManager._currentState != GameManager.GameState.Event)
        {
            if (!(StateManager.CurrentState is BoostState)) MoveController.BoostSpeedCorrection(PlayerStatus);

            StateManager.CurrentState.Execute(this, inputInfo);
        }
    }
    
    //ラッパー関数と呼ぶ
    public void Transition(ICharacterState fromState, ICharacterState nextState)
    {
        StateManager.Transition(this, fromState, nextState);
    }

    //清水 damageRate：HP の減りの倍率（木・看板など細い物は小さい）。減速はいつも同じ
    public void Damage(float damageRate = 1f)
    {
        SetInvincibleTime(2.0f);

        Physics.SetVelocity(MoveController.Deceleration(Physics.GetVelocity(),PlayerStatus));

        CalculationHP(damageRate);

        if (StateManager.CurrentState is GroundMoveState||
            StateManager.CurrentState is LandingState||
            StateManager.CurrentState is BoostLandingState) Transition(StateManager.CurrentState, new GroundDamageState());

        else if (StateManager.CurrentState is AirMoveState||
            StateManager.CurrentState is AirDiveState) Transition(StateManager.CurrentState, new AirDamageState());
    }

    private void CalculationHP(float damageRate)
    {
        int decreaseHp = (int)Physics.GetVelocity().magnitude - Mathf.FloorToInt(PlayerStatus.PERSEVERANCE_POWER);

        hp -= Mathf.Max(1, Mathf.RoundToInt((GuaranteedDamage + decreaseHp) * damageRate));

        PlayerUIManager.Damage(hp);

        if(hp <= 0) FindFirstObjectByType<GameManager>().SetState(GameManager.GameState.GameOver) ;
    }

    //清水 体力を回復する（チェックポイントなど）。実際に回復した量を返す
    public int Heal(int amount)
    {
        int before = hp;
        hp = Mathf.Min(hp + amount, MAX_HP);
        PlayerUIManager.Damage(hp);
        return hp - before;
    }

    public void DeadDamage()    //一撃死亡
    {
        PlayerUIManager.Damage(0);
        FindFirstObjectByType<GameManager>().SetState(GameManager.GameState.GameOver);
    }

    //無敵状態を設定する関数
    public void SetInvincibleTime(float invincibleTime)
    {
        _invincibleUntil = Mathf.Max(_invincibleUntil, Time.time + invincibleTime);
    }

    public void PausePlayer()
    {
        Animation.SetAnimationSpeed(0);
        prevSpeed = Physics.GetVelocity();
        Physics.SetVelocity(Vector3.zero);
    }

    public void ReStartPlayer()
    {
        Animation.SetAnimationSpeed(1);
        Physics.SetVelocity(prevSpeed);
    }
}
