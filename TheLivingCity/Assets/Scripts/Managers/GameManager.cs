using UnityEngine;
using UnityEngine.SceneManagement;

//安井
//ゲームシーン処理
public class GameManager : MonoBehaviour
{
    [SerializeField] private Vector3 StartPos;
    [SerializeField] private GameObject PauseImage;

    //清水 このシーンのモード。Main はエンドレス、StoryGameScene はストーリー
    [SerializeField] private PlayModeInfo.Mode Mode = PlayModeInfo.Mode.Story;
    public bool IsEndless => Mode == PlayModeInfo.Mode.Endless;

    private TimeManager TimeManager;
    private CountDownManager CountDownManager;
    private ScoreManager ScoreManager;

    private Player Player;

    private float PLAYER_DIS;

    private bool isGameOver = false;
    private bool isGameClear = false;

    public enum GameState
    {
        Ready,
        Playing,
        Pause,
        GameClear,
        GameOver,
        Event      //清水 ボス登場などの演出中。プレイヤーも時間も止まる
    }

    public GameState _currentState;

    private void Awake()
    {
        PlayModeInfo.Current = Mode;
        GameOverCause.Reset(); //清水 前の回のやられ方を消す

        TimeManager = GetComponentInChildren<TimeManager>();
        CountDownManager = GetComponentInChildren<CountDownManager>();
        ScoreManager = GetComponentInChildren<ScoreManager>();

        Player = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        UpdateState();
    }

    /// <summary>
    /// 現在の状態に応じて処理を行う
    /// </summary>
    private void UpdateState()
    {
        if (_currentState == GameState.Ready)
        {
            PauseImage.SetActive(false);
            CountDownManager.CountDown();
            if (CountDownManager.CountFinishJudge())
            {
                StartPos = Player.transform.position;
                _currentState = GameState.Playing;
            }
        }
        else if (_currentState == GameState.Playing)
        {
            PauseImage.SetActive(false);
            TimeManager.RemainingTime();
            if (TimeManager.IsTimeUp()) _currentState = GameState.GameOver;
            if (PlayerPosJudge()) _currentState = GameState.GameOver;
        }
        else if (_currentState == GameState.Pause)
        {
            PauseImage.SetActive(true);
        }
        else if (_currentState == GameState.GameClear)
        {
            if (!isGameClear) GameClear();
        }
        else if (_currentState == GameState.GameOver)
        {
            if (!isGameOver) GameOver();
        }
        else if (_currentState == GameState.Event)
        {
            PauseImage.SetActive(false);
        }
    }

    /// <summary>
    /// ステートを移行させる(移行するステート先)
    /// </summary>
    public void SetState(GameState gameState)
    {
        _currentState = gameState;
    }

    /// <summary>
    /// 距離計算
    /// </summary>
    private void GetPlayerDistance()
    {
        Vector3 dis = Player.transform.position - StartPos;
        PLAYER_DIS = dis.magnitude;
    }

    /// <summary>
    /// プレイヤーが落ちたか
    /// </summary>
    private bool PlayerPosJudge()
    {
        return Player.transform.position.y < 0f;
    }

    private void GameOver()
    {
        isGameOver = true;

        GetPlayerDistance();
        ScoreManager.ScoreCalculation(PLAYER_DIS, TimeManager._remainingTime, false);
        ScoreManager.SetScore();
        //清水 プレス機・バーナー・電撃でやられたときは、それぞれの演出のシーンへ（HP切れなどはいつものシーン）
        string scene = GameOverCause.SceneName;
        GameOverCause.Reset();
        FindFirstObjectByType<FadeImageController>().SceneChange(scene);
    }

    private void GameClear()
    {
        isGameClear = true;

        GetPlayerDistance();
        ScoreManager.ScoreCalculation(PLAYER_DIS, TimeManager._remainingTime, true);
        ScoreManager.SetScore();
        FindFirstObjectByType<FadeImageController>().SceneChange("ResultScene");
    }
}