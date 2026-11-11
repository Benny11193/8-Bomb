using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CanvasGroup))]
public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }
    private Animator animator;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        animator = GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PlayTransitionAnimation(bool isFadeIn)
    {
        if (isFadeIn)
        {
            animator.Play("BlackBoardFadeIn");
        }
        else
        {
            animator.Play("BlackBoardFadeOut");
        }
    }

    public void LoadToScene(string nextSceneName)
    {
        StartCoroutine(LoadSceneRoutine(nextSceneName));
    }

    IEnumerator LoadSceneRoutine(string nextSceneName)
    {
        canvasGroup.blocksRaycasts = true;
        yield return new WaitForSecondsRealtime(1.0f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        canvasGroup.blocksRaycasts = false;
    }
}
