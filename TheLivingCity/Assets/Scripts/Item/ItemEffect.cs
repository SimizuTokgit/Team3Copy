using UnityEngine;

public class ItemEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // 再生が終わっていたら破棄
        if (ps != null && !ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
