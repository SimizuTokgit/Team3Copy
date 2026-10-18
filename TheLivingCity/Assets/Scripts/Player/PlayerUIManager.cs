using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    private MaterImageManager materImageManager;
    private MaterTextManager materTextManager;

    private HPBerManager hpBarManager;

    public void Init(int HP)
    {
        materImageManager = FindFirstObjectByType<MaterImageManager>();
        materTextManager = FindFirstObjectByType<MaterTextManager>();
        hpBarManager = FindFirstObjectByType<HPBerManager>();
        hpBarManager.Init(HP);
    }

    public void Damage(int hp)
    {
        hpBarManager.CalculationFillAmount(hp);
    }
}
