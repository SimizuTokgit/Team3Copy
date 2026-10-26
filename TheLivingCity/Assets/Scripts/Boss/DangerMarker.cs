using UnityEngine;

//清水
//地面に出す半透明の「ここは危ない」しるし（プレス機の下、大きい電車が来る列など）
//SetAlert で強く点滅させる
public class DangerMarker : MonoBehaviour
{
    private static Material sharedMaterial;

    private Renderer _renderer;
    private MaterialPropertyBlock _block;
    private Color _color;
    private bool _alert;

    /// <summary>
    /// parent の下に、地面と平行な四角いしるしを作る（size は 横幅x・奥行きz）
    /// </summary>
    public static DangerMarker Create(Transform parent, Vector3 localCenter, Vector2 size, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        DestroyImmediate(obj.GetComponent<Collider>()); //当たり判定はいらない
        obj.name = "DangerMarker";
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localCenter;
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); //上を向ける
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        Renderer r = obj.GetComponent<Renderer>();
        r.sharedMaterial = GetMaterial();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        DangerMarker marker = obj.AddComponent<DangerMarker>();
        marker._renderer = r;
        marker._color = color;
        return marker;
    }

    //Sprites/Default はビルドにも必ず入っていて、半透明・両面で描ける
    private static Material GetMaterial()
    {
        if (!sharedMaterial) sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        return sharedMaterial;
    }

    public void SetAlert(bool alert)
    {
        _alert = alert;
    }

    private void Update()
    {
        if (_block == null) _block = new MaterialPropertyBlock();

        //ふだんはうっすら、危ないときは速く強く点滅
        float a = _alert ? Mathf.Lerp(0.25f, 0.7f, Mathf.PingPong(Time.time * 8f, 1f))
                         : Mathf.Lerp(0.08f, 0.2f, Mathf.PingPong(Time.time * 1.5f, 1f));
        Color c = _color;
        c.a = a;
        _renderer.GetPropertyBlock(_block);
        _block.SetColor("_Color", c);
        _renderer.SetPropertyBlock(_block);
    }
}
