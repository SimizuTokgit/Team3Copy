using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Player _player;
    private PlayerInput _playerInput;

    private InputInfo _inputInfo;
    public InputInfo InputInfo => _inputInfo;

    private InputAction diveAction;

    private GameManager _gameManager;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _playerInput = GetComponent<PlayerInput>();
        diveAction = _playerInput.actions["Dive"];

        _inputInfo = new InputInfo();

        _gameManager = FindAnyObjectByType<GameManager>();
    }
    void Update()
    {
        _inputInfo.IsDiveHeld = diveAction.IsPressed(); //急降下の長押し判定

        _player.Execute(_inputInfo);
        InputClear();
    }

    private void InputClear()
    {
        //Debug.Log(_inputInfo.IsDiveHeld);
    }

    private void OnMove(InputValue value)
    {
        var input = value.Get<Vector2>();
        _inputInfo.Move = new Vector3(input.x, 0.0f, 0.0f);
    }

    private GameManager.GameState prevState;
    private void OnPause(InputValue value)
    {
        //清水 Esc でポーズ画面を開く / もう一度押すとゲームに戻る（ポーズ画面の Back ボタンと同じ）
        if (_gameManager._currentState == GameManager.GameState.Pause) Resume();
        else Pause();
    }

    /// <summary>
    /// 清水 ポーズする。遊んでいる最中だけ
    /// （演出中・3,2,1のカウント中・ゲームオーバー/クリアのあと・チュートリアルの説明中はしない）
    /// </summary>
    public void Pause()
    {
        if (_gameManager._currentState != GameManager.GameState.Playing) return;
        if (Time.timeScale <= 0f) return; //ほかで時間を止めているとき（チュートリアルの説明画面など）

        prevState = _gameManager._currentState;
        _player.PausePlayer();
        _gameManager.SetState(GameManager.GameState.Pause);
    }

    /// <summary>
    /// 清水 ポーズをやめてゲームに戻る（Esc / ポーズ画面の Back ボタン）
    /// </summary>
    public void Resume()
    {
        if (_gameManager._currentState != GameManager.GameState.Pause) return;
        if (PauseMenu.IsQuitting) return; //タイトルに戻る途中

        _gameManager.SetState(prevState);
        _player.ReStartPlayer();
    }
}
