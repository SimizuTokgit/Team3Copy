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
        Alternate, //左右半分ずつずらして噴く。左右とタイミングを読む
        Random     //1本ずつばらばらのタイミングで噴く（休みの長さも毎回変わる）
    }

    private const string FireEffectName = "Burner_Fire"; //炎のパーティクルの名前
    private const float DefaultFireStart = 2.1f;         //見つからなかったときの炎の開始（秒）
    private const float DefaultFireLength = 2.1f;
    private const float OffTime = 1.4f;                  //炎が消えてから次の予兆までの休み（AllAtOnce / Alternate）
    private const float RandomOffMin = 0.6f;             //Random のときの休みの長さ（最小〜最大）
    private const float RandomOffMax = 3.0f;
    private const float VisualScale = 1.8f;              //炎の見た目の大きさ（当たり判定の大きさは変えない）
    private const float HitMargin = 0.1f;                //炎の出始めと終わりは少しだけ当たらない

    private class Burner
    {
        public List<ParticleSystem> Effects; //いちばん上のパーティクル（子もまとめて再生する）
        public Collider[] Colliders;
        public float Offset;
        public float PrevTime = -1f;
        public float Cycle;        //この本の1周の長さ（Random のときは毎回変わる）
        public float CycleStart;   //この周が始まった時間
    }

    private Pattern _pattern;

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
        _pattern = pattern;

        float roadWidth = chunk.ChunkWidth;
        float localZ = transform.localPosition.z;
        int count = Mathf.Max(2, Mathf.FloorToInt(roadWidth / spacing));
        float step = roadWidth / count;

        for (int i = 0; i < count; i++)
        {
            float x = step * (i + 0.5f);
            GameObject obj = Instantiate(burnerPrefab, transform);
            obj.transform.localPosition = new Vector3(x, chunk.GetLocalHeight(x, localZ) - transform.localPosition.y, 0f);
            ModelSlot.EnsureCollider(obj, true); //新しいモデルでも当たるように
            EnlargeVisual(obj, VisualScale);

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
            SetHit(b, false);

            float t;
            if (_pattern == Pattern.Random)
            {
                //1本ずつ周の長さと始まりをばらばらにする
                b.Cycle = NextRandomCycle();
                t = UnityEngine.Random.Range(0f, b.Cycle);
                b.CycleStart = _time - t;
            }
            else
            {
                b.Offset *= _cycle; //半周ずらす
                b.Cycle = _cycle;
                t = Mathf.Repeat(_time - b.Offset, _cycle);
                b.CycleStart = _time - t;
            }

            //途中から始まるときは、エフェクトもその時間まで進めておく
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
            float t = _time - b.CycleStart;

            //1周したらエフェクトを最初から流し直す（Random は次の周の長さを決め直す）
            if (t >= b.Cycle)
            {
                b.CycleStart += b.Cycle;
                if (_pattern == Pattern.Random) b.Cycle = NextRandomCycle();
                t = _time - b.CycleStart;

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

    private float NextRandomCycle()
    {
        return _fireStart + _fireLength + UnityEngine.Random.Range(RandomOffMin, RandomOffMax);
    }

    //炎の見た目だけ大きくする。当たり判定は元の大きさのまま
    private static void EnlargeVisual(GameObject obj, float scale)
    {
        obj.transform.localScale *= scale;

        //パーティクルが親の大きさに合わせて大きくなるようにする
        foreach (ParticleSystem ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        //当たり判定は大きくした分だけ小さくして、元の大きさに戻す
        foreach (Collider col in obj.GetComponentsInChildren<Collider>(true))
        {
            if (col is BoxCollider box)
            {
                box.size /= scale;
                box.center /= scale;
            }
            else if (col is CapsuleCollider capsule)
            {
                capsule.radius /= scale;
                capsule.height /= scale;
                capsule.center /= scale;
            }
            else if (col is SphereCollider sphere)
            {
                sphere.radius /= scale;
                sphere.center /= scale;
            }
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
