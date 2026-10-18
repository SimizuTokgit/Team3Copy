using System.Collections.Generic;
using UnityEngine;

//清水
//道幅いっぱいに並べたガスバーナー（中ボス3の攻撃）
//炎が出ている間に触れたら一撃死。止まっている間に抜ける
public class BurnerHazard : MonoBehaviour
{
    public enum Pattern
    {
        AllAtOnce, //全部いっしょに噴く。タイミングだけで抜ける
        Alternate  //左右半分ずつ交互に噴く。左右とタイミングを読む
    }

    [SerializeField] private Pattern FirePattern = Pattern.Alternate;
    [SerializeField] private float Cycle = 3.0f;        //1周の秒数
    [SerializeField] private float FireTime = 1.2f;     //炎が出ている秒数
    [SerializeField] private float HitDelay = 0.2f;     //炎が見えてから当たり判定が出るまで

    private readonly List<GameObject> _burners = new List<GameObject>();
    private readonly List<Collider[]> _colliders = new List<Collider[]>();
    private readonly List<ParticleSystem[]> _effects = new List<ParticleSystem[]>();
    private readonly List<bool> _isFiring = new List<bool>();

    private float _time;

    /// <summary>
    /// バーナーを並べる（BossBattleManager から呼ぶ）
    /// </summary>
    public void Setup(GameObject burnerPrefab, float roadWidth, float spacing, Pattern pattern, float startOffset)
    {
        FirePattern = pattern;
        _time = startOffset;

        int count = Mathf.Max(2, Mathf.FloorToInt(roadWidth / spacing));
        float step = roadWidth / count;

        for (int i = 0; i < count; i++)
        {
            GameObject burner = Instantiate(burnerPrefab, transform);
            burner.transform.localPosition = new Vector3(step * (i + 0.5f), 0, 0);

            BossHazard.MakeDeadly(burner);

            _burners.Add(burner);
            _colliders.Add(burner.GetComponentsInChildren<Collider>(true));
            _effects.Add(burner.GetComponentsInChildren<ParticleSystem>(true));
            _isFiring.Add(true);

            SetFire(i, false);
        }
    }

    private void Update()
    {
        _time += Time.deltaTime;

        float half = _burners.Count / 2f;
        for (int i = 0; i < _burners.Count; i++)
        {
            //交互のときは右半分を半周ずらす
            float offset = (FirePattern == Pattern.Alternate && i >= half) ? Cycle * 0.5f : 0f;
            float t = Mathf.Repeat(_time - offset, Cycle);

            SetFire(i, t < FireTime);
            SetHit(i, t >= HitDelay && t < FireTime);
        }
    }

    private void SetFire(int index, bool fire)
    {
        if (_isFiring[index] == fire) return;
        _isFiring[index] = fire;

        foreach (ParticleSystem effect in _effects[index])
        {
            if (fire) effect.Play();
            else effect.Stop();
        }
    }

    private void SetHit(int index, bool hit)
    {
        foreach (Collider col in _colliders[index])
        {
            col.enabled = hit;
        }
    }
}
