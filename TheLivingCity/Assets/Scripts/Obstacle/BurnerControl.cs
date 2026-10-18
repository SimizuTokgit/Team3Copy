using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BurnerControl : MonoBehaviour
{
    //バーナーを自動で入れる
    [SerializeField]
    private List<GameObject> Burner = new List<GameObject>();

    //バーナーのコライダーが入る
    [SerializeField]
    private List<BoxCollider> BurnerCollider = new List<BoxCollider>();

    private int BurnerCount;

    private List<List<ParticleSystem>> burnerEffects = new List<List<ParticleSystem>>();

    //現在前半を再生しているか
    private bool isFirstHalf = true;

    //前半・後半共通の再生時間
    [SerializeField]
    private float burnerDuration;

    //現在の再生時間
    private float burnerTime = 0f;

    //バーナーの当たり判定がonになる時間
    private float burnerCollidrTime = 5.0f;

    //前半部分
    public float firstColliderTime;
    public bool OnfirstCollider;

    //後半部分
    public float secondColliderTime;
    public bool OnsecondCollider;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Burner.Clear();
        BurnerCollider.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Burner.Add(transform.GetChild(i).gameObject);
        }

        BurnerCount = Burner.Count;

        for(int i = 0;i < transform.childCount; i++)
        {
            BurnerCollider.Add(Burner[i].GetComponent<BoxCollider>());

            BurnerCollider[i].enabled = false;
        }

        //各バーナーからParticleSystemを取得
        foreach (GameObject burner in Burner)
        {
            List<ParticleSystem> effects = new List<ParticleSystem>();

            //階層の深さに関係なくすべて取得
            ParticleSystem[] particleSystems = burner.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem effect in particleSystems)
            {
                effects.Add(effect);
            }

            burnerEffects.Add(effects);
        }

        OnfirstCollider = false;
        OnsecondCollider = false;

        PlayFirstHalf();
    }

    // Update is called once per frame
    void Update()
    {
        //再生時間を加算
        burnerTime += Time.deltaTime;

        //設定時間に到達したら前半・後半を切り替える
        if (burnerTime >= burnerDuration)
        {
            if (isFirstHalf)
            {
                //前半終了 → 後半
                PlaySecondHalf();
            }
            else
            {
                //後半終了 → 前半
                PlayFirstHalf();
            }
        }

        if (OnfirstCollider)
        {
            firstColliderTime += Time.deltaTime;

            if (firstColliderTime >= burnerCollidrTime)
            {
                OnfirstCollider = false;
                OffFirstCollider();
            }
        }
        else
        {
            firstColliderTime = 0.0f;
        }

        if (OnsecondCollider)
        {
            secondColliderTime += Time.deltaTime;

            if (secondColliderTime >= burnerCollidrTime)
            {
                OnsecondCollider = false;
                OffSecondCollider();
            }
        }
        else
        {
            secondColliderTime = 0.0f;
        }
    }

    //バーナー前半分のエフェクト再生
    void PlayFirstHalf()
    {
        StopFirstHalfEffects();
        OnfirstCollider = true;

        int half = BurnerCount / 2;

        for (int i = 0; i < burnerEffects.Count; i++)
        {
            if (i < half)
            {
                PlayBurnerEffects(i);
                BurnerCollider[i].enabled = true;
            }
        }

        //再生時間をリセット
        burnerTime = 0f;

        isFirstHalf = true;
    }


    //バーナー後半分のエフェクト再生
    void PlaySecondHalf()
    {
        StopSecondHalfEffects();
        OnsecondCollider = true;

        int half = BurnerCount / 2;

        for (int i = 0; i < burnerEffects.Count; i++)
        {
            if (i >= half)
            {
                PlayBurnerEffects(i);
                BurnerCollider[i].enabled = true;
            }
        }

        //再生時間をリセット
        burnerTime = 0f;

        isFirstHalf = false;
    }

    //指定したバーナーのエフェクトをすべて再生
    void PlayBurnerEffects(int burnerIndex)
    {
        foreach (ParticleSystem effect in burnerEffects[burnerIndex])
        {
            effect.Play();
        }
    }

    //前半のバーナーエフェクトを停止
    void StopFirstHalfEffects()
    {
        int half = BurnerCount / 2;

        for (int i = 0; i < half; i++)
        {
            foreach (ParticleSystem effect in burnerEffects[i])
            {
                effect.Stop();
            }
        }
    }


    //後半のバーナーエフェクトを停止
    void StopSecondHalfEffects()
    {
        int half = BurnerCount / 2;

        for (int i = half; i < BurnerCount; i++)
        {
            foreach (ParticleSystem effect in burnerEffects[i])
            {
                effect.Stop();
            }
        }
    }

    //前半のバーナーの当たり判定を消す
    private void OffFirstCollider()
    {
        int half = BurnerCount / 2;

        for (int i = 0; i < half; i++)
        {
            BurnerCollider[i].enabled = false;
        }
    }

    //後半のバーナーの当たり判定を消す
    private void OffSecondCollider()
    {
        int half = BurnerCount / 2;

        for (int i = half; i < BurnerCount; i++)
        {
            BurnerCollider[i].enabled = false;
        }
    }
}
