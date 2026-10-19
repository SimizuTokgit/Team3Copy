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
        //清水 演出中はポーズさせない
        if (_gameManager._currentState == GameManager.GameState.Event) return;

        if (_gameManager._currentState != GameManager.GameState.Pause)
        {
            prevState = _gameManager._currentState;
            _player.PausePlayer();
            _gameManager.SetState(GameManager.GameState.Pause);
        }


        else
        {
            _gameManager.SetState(prevState);
            _player.ReStartPlayer();
        }
    }
}
