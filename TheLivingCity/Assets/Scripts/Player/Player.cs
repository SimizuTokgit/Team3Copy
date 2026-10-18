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

    private readonly int GuaranteedDamage = 10;     //絶対受ける最低ダメージ

    public bool isInvincible { get; private set; } = false;

    private Vector3 prevSpeed = Vector3.zero;

    //チュートリアル
    public TutorialManager tutorialManager;

    private void Awake()
    {
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
        if (GameManager._currentState != GameManager.GameState.Pause)
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

    public void Damage()
    {
        SetInvincibleTime(2.0f);

        Physics.SetVelocity(MoveController.Deceleration(Physics.GetVelocity(),PlayerStatus));

        CalculationHP();

        if (StateManager.CurrentState is GroundMoveState||
            StateManager.CurrentState is LandingState||
            StateManager.CurrentState is BoostLandingState) Transition(StateManager.CurrentState, new GroundDamageState());

        else if (StateManager.CurrentState is AirMoveState||
            StateManager.CurrentState is AirDiveState) Transition(StateManager.CurrentState, new AirDamageState());
    }

    private void CalculationHP()
    {
        int decreaseHp = (int)Physics.GetVelocity().magnitude - PlayerStatus.PERSEVERANCE_POWER;

        hp -= GuaranteedDamage + decreaseHp;

        PlayerUIManager.Damage(hp);

        if(hp <= 0) FindFirstObjectByType<GameManager>().SetState(GameManager.GameState.GameOver) ;
    }

    public void DeadDamage()    //一撃死亡
    {
        PlayerUIManager.Damage(0);
        FindFirstObjectByType<GameManager>().SetState(GameManager.GameState.GameOver);
    }

    //無敵状態を設定する関数
    public void SetInvincibleTime(float invincibleTime)
    {
        isInvincible = true;
        Invoke(nameof(ResetIsInvincible), invincibleTime);
    }

    private void ResetIsInvincible()
    {
        isInvincible = false;
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
