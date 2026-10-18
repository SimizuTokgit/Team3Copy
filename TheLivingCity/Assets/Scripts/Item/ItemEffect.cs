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
        // Ä¶‚ªI‚í‚Á‚Ä‚¢‚½‚ç”jŠü
        if (ps != null && !ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
