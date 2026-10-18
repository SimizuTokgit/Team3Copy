using System.Collections;
using UnityEngine;

public class MotherComputer : MonoBehaviour
{
    [SerializeField] private int MAX_HP = 10;
    private GameManager GameManager;

    //清水 ストーリーで何体も出すときの進行役。いなければ倒した時点でクリア
    private BossBattleManager BattleManager;

    //清水 プレイヤーにどつかれずに追い越されたら、前に回り込む
    [SerializeField] private float OVERTAKEN_DISTANCE = 30f;
    private Transform PlayerTransform;

    private int _hp;

    public int MaxHp => MAX_HP;

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

        Player player = FindAnyObjectByType<Player>();
        if (player) PlayerTransform = player.transform;
    }

    public void SetBattleManager(BossBattleManager battleManager)
    {
        BattleManager = battleManager;
    }

    private void Update()
    {
        //清水 追い越されたままだと二度と当てられないので、ダメージ無しで前へ跳ぶ
        if (!_isMoving && PlayerTransform && PlayerTransform.position.z > transform.position.z + OVERTAKEN_DISTANCE)
        {
            StartJump();
            return;
        }

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

            //清水 当てた手ごたえ（一瞬止める・揺らす・光らせる・ボスを点滅）
            ScreenEffects.Get().HitStop(0.12f);
            ScreenEffects.Get().Shake(1.2f, 0.45f);
            ScreenEffects.Get().FlashExposure(1.2f);
            GameHUD.Get().Flash(new Color(1f, 1f, 1f, 0.6f), 0.2f);
            StartCoroutine(Blink());

            // HPが0になったらゲームクリア
            if (_hp <= 0)
            {
                _isMoving = true; //倒された後にもう一度当たらないように
                if (BattleManager) BattleManager.OnBossDefeated(this);
                else GameManager._currentState = GameManager.GameState.GameClear;
                return;
            }

            if (BattleManager) BattleManager.OnBossHit(this, _hp, MAX_HP);

            StartJump();
        }
    }

    //清水 当てられたら赤く点滅する
    private IEnumerator Blink()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        for (int i = 0; i < 6; i++)
        {
            Color c = (i % 2 == 0) ? new Color(1f, 0.2f, 0.2f) : Color.white;
            foreach (Renderer r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(block);
            }
            yield return new WaitForSecondsRealtime(0.08f);
        }

        foreach (Renderer r in renderers)
        {
            if (r) r.SetPropertyBlock(null);
        }
    }

    //清水 前へアーチ移動する処理を、追い越されたときにも使えるように関数にした
    private void StartJump()
    {
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