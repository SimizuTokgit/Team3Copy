using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;

public class MaterMask : MonoBehaviour
{
    private Image image;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();
    }

    public void SetFillAmount(float amount)
    { 
        image.fillAmount = Mathf.MoveTowards(image.fillAmount, amount, Time.deltaTime * 10);
    }

    public void IsOver()
    { 
        image.fillAmount = 1;
    }

    private float itemMaskAmount = 0;
    public bool GetItem(float addAmount)
    {
        if (itemMaskAmount >= 1) return true;

        itemMaskAmount += addAmount;
        image.fillAmount = Mathf.MoveTowards(image.fillAmount, itemMaskAmount, Time.deltaTime * 20);

        return false;

        
    }

}
