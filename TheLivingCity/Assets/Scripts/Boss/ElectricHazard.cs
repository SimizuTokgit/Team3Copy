using UnityEngine;

//清水
//感電する触手が巻き付いたオブジェクト（中ボス2の攻撃）
//触れたら一撃死。青白く点滅させて危ないことを見せる
public class ElectricHazard : MonoBehaviour
{
    [SerializeField] private Color ElectricColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private float LightRange = 25f;
    [SerializeField] private float LightIntensity = 8f;
    [SerializeField] private float FlickerSpeed = 18f;   //点滅の速さ
    [SerializeField] private float LightHeight = 6f;     //ライトを置く高さ

    private Light _light;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _block;

    private void Start()
    {
        //清水 当たり判定が無いモデル（触手だけのモデルなど）でも当たるようにしてから一撃死にする
        ModelSlot.EnsureCollider(gameObject, true);
        BossHazard.MakeDeadly(gameObject);

        GameObject lightObj = new GameObject("ElectricLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0, LightHeight, 0);
        _light = lightObj.AddComponent<Light>();
        _light.type = LightType.Point;
        _light.color = ElectricColor;
        _light.range = LightRange;

        _renderers = GetComponentsInChildren<Renderer>();
        _block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        //バチバチっぽく不規則に光らせる
        float noise = Mathf.PerlinNoise(Time.time * FlickerSpeed, transform.position.z);
        float flash = noise > 0.55f ? 1f : 0.2f;

        _light.intensity = LightIntensity * flash;

        Color tint = Color.Lerp(Color.white, ElectricColor, flash);
        foreach (Renderer r in _renderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", tint);
            r.SetPropertyBlock(_block);
        }
    }
}
