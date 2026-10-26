
using System.Collections.Generic;
using UnityEngine;

// 清水
// プレス機
// 上で停止 → 予告 → 振り下ろし → 下で停止 → 上昇
// 振り下ろし中と下で停止している間だけ当たり判定を有効にする
public class PressHazard : MonoBehaviour
{
    [Header("モデル")]
    [SerializeField] private Material ContainerMaterial;
    [SerializeField] private float ContainerHeight = 4.7f;

    [Header("当たり判定")]
    [SerializeField] private BoxCollider _crush;

    [Header("動き（秒）")]
    [SerializeField] private float UpTime = 1.2f;
    [SerializeField] private float WarnTime = 0.6f;
    [SerializeField] private float SlamTime = 0.2f;
    [SerializeField] private float DownTime = 1.0f;
    [SerializeField] private float RiseTime = 1.0f;

    [Header("演出")]
    [SerializeField] private float WarnShake = 0.25f;
    [SerializeField] private float ShakeDistance = 45f;

    private Transform _model;
    private Vector3 _modelBase;
    private DangerMarker _marker;
    private Transform _player;

    private float _bottom;
    private float _time;
    private bool _slammed;

    private float Cycle =>
        Mathf.Max(0.01f, UpTime) +
        Mathf.Max(0.01f, WarnTime) +
        Mathf.Max(0.01f, SlamTime) +
        Mathf.Max(0.01f, DownTime) +
        Mathf.Max(0.01f, RiseTime);

    public const float ModelWidth = 10.9f;
    public const float ModelDepth = 4.6f;

    /// <summary>
    /// 道幅いっぱいにプレス機を並べる
    /// 隣同士は半周期ずらす
    /// </summary>
    public static List<GameObject> SpawnRow(
        GameObject prefab,
        GroundAutomatically chunk,
        float localZ,
        int count,
        float phase,
        int itemsPerPress)
    {
        List<GameObject> list = new List<GameObject>();

        if (!prefab) return list;

        count = Mathf.Max(1, count);

        float section = chunk.ChunkWidth / count;
        float scale = section / ModelWidth;

        // 配置場所の障害物を削除
        if (chunk.Layout != null)
        {
            chunk.Layout.ClearArea(
                new Rect(
                    0f,
                    localZ - ModelDepth * scale,
                    chunk.ChunkWidth,
                    ModelDepth * scale * 2f
                )
            );
        }

        for (int i = 0; i < count; i++)
        {
            float x = section * (i + 0.5f);

            GameObject press = Instantiate(prefab, chunk.transform);

            press.transform.localPosition = new Vector3(
                x,
                chunk.GetLocalHeight(x, localZ),
                localZ
            );

            press.transform.localScale *= scale;

            PressHazard hazard =
                press.GetComponentInChildren<PressHazard>();

            if (hazard)
            {
                hazard.SetPhase(
                    Mathf.Repeat(
                        phase + (i % 2) * 0.5f,
                        1f
                    )
                );
            }

            // アイテム生成
            ItemGenerate items = chunk._itemGenerate;

            if (items && itemsPerPress > 0)
            {
                float d = ModelDepth * scale;

                items.SpawnLine(
                    chunk,
                    new Vector3(x, 0f, localZ - d),
                    new Vector3(x, 0f, localZ + d),
                    itemsPerPress
                );
            }

            list.Add(press);
        }

        return list;
    }

    /// <summary>
    /// 動作開始位置を設定（0～1）
    /// </summary>
    public void SetPhase(float phase01)
    {
        _time = Mathf.Repeat(phase01, 1f) * Cycle;
    }

    private void Start()
    {
        // モデル取得
        _model = transform.childCount > 0
            ? transform.GetChild(0)
            : transform;

        _modelBase = _model.localPosition;

        // マテリアル設定
        Renderer[] renderers =
            _model.GetComponentsInChildren<Renderer>();

        if (ContainerMaterial)
        {
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = ContainerMaterial;
                }

                r.sharedMaterials = mats;
            }
        }

        // モデルの大きさを取得
        Vector3 min = new Vector3(-5f, 7f, -2.3f);
        Vector3 max = new Vector3(5f, 11.7f, 2.3f);

        bool found = false;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z
                );

                Vector3 local =
                    _model.InverseTransformPoint(corner);

                if (!found)
                {
                    min = max = local;
                    found = true;
                }

                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }

        min.y = Mathf.Max(
            min.y,
            max.y - ContainerHeight
        );

        _bottom = Mathf.Max(
            0f,
            _modelBase.y + min.y * _model.localScale.y
        );

        // コンテナに付いているColliderを取得
        if (_crush == null)
        {
            _crush = _model.GetComponentInChildren<BoxCollider>();
        }

        if (_crush == null)
        {
            Debug.LogError(
                "PressHazard: BoxColliderが設定されていません",
                this
            );

            enabled = false;
            return;
        }

        // Triggerとして使用
        _crush.isTrigger = true;
        _crush.enabled = false;

        // 既存の即死処理
        BossHazard.MakeDeadly(gameObject);

        // MakeDeadlyがCollider設定を変更する場合に備える
        _crush.isTrigger = true;
        _crush.enabled = false;

        // 危険マーカー
        Vector3 center =
            transform.InverseTransformPoint(
                _crush.transform.TransformPoint(_crush.center)
            );

        Vector3 size = _crush.bounds.size;

        _marker = DangerMarker.Create(
            transform,
            new Vector3(center.x, 0.3f, center.z),
            new Vector2(size.x, size.z),
            GameHUD.Danger
        );

        // プレイヤー取得
        Player player = FindAnyObjectByType<Player>();

        if (player)
        {
            _player = player.transform;
        }
    }

    private void Update()
    {
        _time += Time.deltaTime;

        float t = Mathf.Repeat(_time, Cycle);

        float drop = 0f;

        bool warn = false;
        bool falling = false;
        bool down = false;

        // 上で停止
        if (t < UpTime)
        {
            _slammed = false;
        }
        // 予告
        else if ((t -= UpTime) < WarnTime)
        {
            warn = true;
        }
        // 振り下ろし
        else if ((t -= WarnTime) < SlamTime)
        {
            float k = Mathf.Clamp01(
                t / Mathf.Max(0.01f, SlamTime)
            );

            drop = k * k;
            falling = true;
        }
        // 下で停止
        else if ((t -= SlamTime) < DownTime)
        {
            drop = 1f;
            down = true;

            if (!_slammed)
            {
                OnSlam();
            }
        }
        // 上昇
        else
        {
            t -= DownTime;

            drop = 1f - Mathf.SmoothStep(
                0f,
                1f,
                t / Mathf.Max(0.01f, RiseTime)
            );
        }

        // 予告中の震え
        Vector3 shake = Vector3.zero;

        if (warn)
        {
            Vector2 r =
                Random.insideUnitCircle * WarnShake;

            shake = new Vector3(
                r.x,
                0f,
                r.y
            );
        }

        // コンテナ移動
        _model.localPosition =
            _modelBase +
            shake +
            Vector3.down * (_bottom * drop);

        // 振り下ろし中と下で停止中だけ有効
        bool deadly = falling || down;

        if (_crush.enabled != deadly)
        {
            _crush.enabled = deadly;
        }

        // 危険マーカー
        if (_marker)
        {
            _marker.SetAlert(
                warn || falling || down
            );
        }
    }

    /// <summary>
    /// 振り下ろし完了時の演出
    /// </summary>
    private void OnSlam()
    {
        _slammed = true;

        if (_player &&
            Vector3.Distance(
                _player.position,
                transform.position
            ) < ShakeDistance)
        {
            ScreenEffects.Get().Shake(
                0.35f,
                0.25f
            );
        }
    }

    private void OnDisable()
    {
        if (_crush)
        {
            _crush.enabled = false;
        }
    }
}
