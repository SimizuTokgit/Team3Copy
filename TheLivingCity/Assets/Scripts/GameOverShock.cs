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
            // numが0なら元の色、1なら黒
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
