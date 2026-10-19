using System.Collections.Generic;
using UnityEngine;

//清水
//道幅いっぱいに並べたガスバーナー（中ボス3の攻撃）
//炎が出ている間に触れたら一撃死。止まっている間に抜ける
//E_Burner は 1回の再生で「予兆(FirePrepare) → 点火 → 炎(Burner_Fire) → 消える」が全部入っているので、
//再生のたびに炎が出ている時間だけ当たり判定を出す
public class BurnerHazard : MonoBehaviour
{
    public enum Pattern
    {
        AllAtOnce, //全部いっしょに噴く。タイミングだけで抜ける
        Alternate  //左右半分ずつずらして噴く。左右とタイミングを読む
    }

    private const string FireEffectName = "Burner_Fire"; //炎のパーティクルの名前
    private const float DefaultFireStart = 2.1f;         //見つからなかったときの炎の開始（秒）
    private const float DefaultFireLength = 2.1f;
    private const float OffTime = 1.4f;                  //炎が消えてから次の予兆までの休み
    private const float HitMargin = 0.1f;                //炎の出始めと終わりは少しだけ当たらない

    private class Burner
    {
        public List<ParticleSystem> Effects; //いちばん上のパーティクル（子もまとめて再生する）
        public Collider[] Colliders;
        public float Offset;
        public float PrevTime = -1f;
    }

    private readonly List<Burner> _burners = new List<Burner>();
    private float _fireStart = DefaultFireStart;
    private float _fireLength = DefaultFireLength;
    private float _cycle;
    private float _time;

    /// <summary>
    /// バーナーを並べる（BossBattleManager から呼ぶ）。地面の高さに合わせて1本ずつ置く
    /// </summary>
    public void Setup(GameObject burnerPrefab, GroundAutomatically chunk, float spacing, Pattern pattern, float startOffset)
    {
        _time = startOffset;

        float roadWidth = chunk.ChunkWidth;
        float localZ = transform.localPosition.z;
        int count = Mathf.Max(2, Mathf.FloorToInt(roadWidth / spacing));
        float step = roadWidth / count;

        for (int i = 0; i < count; i++)
        {
            float x = step * (i + 0.5f);
            GameObject obj = Instantiate(burnerPrefab, transform);
            obj.transform.localPosition = new Vector3(x, chunk.GetLocalHeight(x, localZ) - transform.localPosition.y, 0f);

            BossHazard.MakeDeadly(obj);

            Burner burner = new Burner
            {
                Effects = FindRootEffects(obj),
                Colliders = obj.GetComponentsInChildren<Collider>(true),
                Offset = (pattern == Pattern.Alternate && i >= count / 2) ? 0.5f : 0f,
            };
            if (i == 0)
            {
                ReadTiming(obj);
                //消えたあとの煙（Burner_FireEnd）は長いので待たずに次の周に入る
                _cycle = _fireStart + _fireLength + OffTime;
            }
            _burners.Add(burner);
        }

        foreach (Burner b in _burners)
        {
            b.Offset *= _cycle; //半周ずらす
            SetHit(b, false);

            //途中から始まるときは、エフェクトもその時間まで進めておく
            float t = Mathf.Repeat(_time - b.Offset, _cycle);
            foreach (ParticleSystem ps in b.Effects)
            {
                ps.Simulate(t, true, true);
                ps.Play(true);
            }
            b.PrevTime = t;
        }
    }

    private void Update()
    {
        _time += Time.deltaTime;

        foreach (Burner b in _burners)
        {
            float t = Mathf.Repeat(_time - b.Offset, _cycle);

            //1周したらエフェクトを最初から流し直す
            if (t < b.PrevTime)
            {
                foreach (ParticleSystem ps in b.Effects)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
            b.PrevTime = t;

            SetHit(b, t >= _fireStart + HitMargin && t < _fireStart + _fireLength - HitMargin);
        }
    }

    private static void SetHit(Burner b, bool hit)
    {
        foreach (Collider col in b.Colliders)
        {
            if (col && col.enabled != hit) col.enabled = hit;
        }
    }

    //炎のパーティクルの開始時間と長さを読む
    private void ReadTiming(GameObject obj)
    {
        foreach (ParticleSystem ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.name != FireEffectName) continue;
            ParticleSystem.MainModule main = ps.main;
            _fireStart = main.startDelay.constant;
            _fireLength = main.duration;
            return;
        }
    }

    //親にパーティクルが無い、いちばん上のパーティクル（子は Play(true) でまとめて動く）
    private static List<ParticleSystem> FindRootEffects(GameObject obj)
    {
        List<ParticleSystem> roots = new List<ParticleSystem>();
        foreach (ParticleSystem ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            Transform parent = ps.transform.parent;
            if (!parent || !parent.GetComponentInParent<ParticleSystem>()) roots.Add(ps);
        }
        return roots;
    }
}
