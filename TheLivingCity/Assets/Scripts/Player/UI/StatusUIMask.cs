using UnityEngine;
using UnityEngine.UI;

public class StatusUIMask : MonoBehaviour
{
    private Image maskImage;

    public void Init(int maxValue)
    {
        maskImage = GetComponent<Image>();
        maskImage.fillAmount = 0;
    }

    public void GetItem()
    {
        maskImage.fillAmount += 0.05f;
    }

}
