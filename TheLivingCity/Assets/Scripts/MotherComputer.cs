using UnityEngine;

public class MotherComputer : MonoBehaviour
{
    [SerializeField] private int MAX_HP = 10;
    private GameManager GameManager;

    private int _hp;

    // アーチ移動設定
    [SerializeField] private float MOVE_DISTANCE = 1000f;
    [SerializeField] private float MOVE_DURATION = 2.0f;
    [SerializeField] private float ARC_HEIGHT = 0f;

    private bool _isMoving = false;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _moveTime;

    private Animator _animator;

    private enum State
    {
        Attack,
        Damage
    }

    private State _state;

    private void Start()
    {
        _hp = MAX_HP;
        GameManager = FindAnyObjectByType<GameManager>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!_isMoving) return;

        if(_state == State.Attack)
        {

        }
        else if(_state == State.Damage)
        {
            _moveTime += Time.deltaTime;

            float t = _moveTime / MOVE_DURATION;
            t = Mathf.Clamp01(t);

            // 開始位置から終了位置へ移動
            Vector3 position = Vector3.Lerp(
                _startPosition,
                _targetPosition,
                t
            );

            // アーチの高さ
            float arc = 4f * ARC_HEIGHT * t * (1f - t);

            position.y += arc;

            transform.position = position;

            // 移動終了
            if (t >= 1f)
            {
                transform.position = _targetPosition;
                _isMoving = false;
                _animator.ResetTrigger("damage");
                _animator.SetTrigger("attack");
                _state = State.Attack;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isMoving) return;

        if (other.GetComponent<Player>())
        {
            --_hp;

            // HPが0になったらゲームクリア
            if (_hp <= 0)
            {
                GameManager._currentState = GameManager.GameState.GameClear;
                return;
            }

            // アーチ移動開始
            _startPosition = transform.position;

            _targetPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z + MOVE_DISTANCE
            );

            _moveTime = 0f;
            _isMoving = true;
            _animator.ResetTrigger("attack");
            _animator.SetTrigger("damage");
            _state = State.Damage;
        }
    }
}