using UnityEngine;

public class OptionManager : MonoBehaviour
{
    private bool isPushButton = false;
    public void BackButton()
    {
        if (!isPushButton)
        {
            FindFirstObjectByType<FadeImageController>().SceneChange("Title");
            isPushButton = true;
        }
    }
}
