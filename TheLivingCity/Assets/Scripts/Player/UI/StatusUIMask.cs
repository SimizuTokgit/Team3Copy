using UnityEngine;
using UnityEngine.UI;

public class StatusUIMask : MonoBehaviour
{
    [SerializeField] MaterMask[] Masks = new MaterMask[4];

    //private bool[] isMaterLimit = new bool[4];

    public void GetItem(float addValue)
    {
        for (int i = 0; i < 4; i++)
        {
            if (!Masks[i].GetItem(addValue))
            {
                return;
            }
        }
    }

}
