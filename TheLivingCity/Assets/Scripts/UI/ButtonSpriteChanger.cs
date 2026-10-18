using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 選択中のボタンだけON画像にする
[RequireComponent(typeof(Image))]
public class ButtonSpriteChanger : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;

    [SerializeField] private AudioClip selectSE; // カーソルが移ったとき
    [SerializeField] private AudioClip pushSE;   // 押したとき

    // 同じフレームで選択が移ってきたかを見る
    private static int deselectFrame = -1;

    private Image image;
    private float pixelScale; // 画像1ピクセルあたりの大きさ

    private void Awake()
    {
        image = GetComponent<Image>();

        // 画像を入れ忘れてもUI全体が止まらないようにしておく
        if (!offSprite) offSprite = image.sprite;
        if (!onSprite) onSprite = offSprite;

        // 幅が0（ストレッチ配置など）のときは大きさを変えない
        float width = image.rectTransform.sizeDelta.x;
        bool canScale = offSprite && width > 0 && offSprite.rect.width > 0;
        pixelScale = canScale ? width / offSprite.rect.width : 0f;

        Button button = GetComponent<Button>();
        if (button) button.onClick.AddListener(() => PlaySE(pushSE));
    }

    private void OnEnable()
    {
        EventSystem eventSystem = EventSystem.current;
        SetSprite(eventSystem && eventSystem.currentSelectedGameObject == gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetSprite(true);

        // 他のボタンから移ってきたときだけ鳴らす
        if (deselectFrame == Time.frameCount) PlaySE(selectSE);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetSprite(false);
        deselectFrame = Time.frameCount;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // マウスでもパッドと同じ見た目になるように選択する
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    private void SetSprite(bool isOn)
    {
        Sprite sprite = isOn ? onSprite : offSprite;
        if (!sprite) return;

        image.sprite = sprite;

        // ON画像は余白が大きいので画素数に合わせて大きさを変える
        if (pixelScale > 0f) image.rectTransform.sizeDelta = sprite.rect.size * pixelScale;
    }

    private void PlaySE(AudioClip clip)
    {
        if (clip && SoundManager.Instance) SoundManager.Instance.PlayAudioClip(clip);
    }
}
