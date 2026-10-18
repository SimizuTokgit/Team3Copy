using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeImageController : MonoBehaviour
{
    private Animator animator;

    private string nextSceneName;

    private void Start()
    {
        animator = GetComponent<Animator>();
        nextSceneName = null;
    }

    public void SceneChange(string SceneName)
    {
        animator.SetTrigger("FadeOut");
        nextSceneName = SceneName;
    }

    public void LoadScene()
    {
        if (nextSceneName != null)
        {
            SceneManager.LoadScene(nextSceneName);
            nextSceneName = null;
        }
    }
}
