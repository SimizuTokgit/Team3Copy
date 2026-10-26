using UnityEngine;

public class GameOverShock : MonoBehaviour
{
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject Electronic;
    [SerializeField] private GameObject Texts;
    [SerializeField] private GameObject Fade;

    public void MaterialBlack(float num)
    {
        Renderer renderer = Player.GetComponent<Renderer>();

        if (renderer != null)
        {
            // numÇ™0Ç»ÇÁå≥ÇÃêFÅA1Ç»ÇÁçï
            renderer.material.color = Color.Lerp(
                Color.white,
                Color.black,
                num
            );
        }
    }

    public void ElectronicGeneret()
    {
        Electronic.SetActive(true);
    }

    public void TextGeneret()
    {
        Texts.SetActive(true);
    }
    public void FadeFalse()
    {
        Fade.SetActive(false);
    }
}
