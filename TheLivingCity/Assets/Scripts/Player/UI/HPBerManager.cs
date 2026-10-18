using UnityEngine;
using UnityEngine.UI;

public class HPBerManager : MonoBehaviour
{
    private Image maskImage;

    private int startHP = 0;

    private float targetFillAmount = 1;

    private readonly float moveSpeed = 1;

    public void Init(int hp)
    {
        maskImage = GetComponent<Image>();

        maskImage.fillAmount = 1; //1Ç™HPMaxèÛë‘
        targetFillAmount = 1;
        startHP = hp;
    }

    public void CalculationFillAmount(int hp)
    {
        if (startHP <= 0) return;
        targetFillAmount = Mathf.Clamp01((float)hp / startHP);
    }

    void Update()
    {
        if (!Mathf.Approximately(maskImage.fillAmount, targetFillAmount))
        {
            maskImage.fillAmount = Mathf.MoveTowards(
                maskImage.fillAmount,
                targetFillAmount,
                Time.deltaTime * moveSpeed
            );
        }
    }
}
