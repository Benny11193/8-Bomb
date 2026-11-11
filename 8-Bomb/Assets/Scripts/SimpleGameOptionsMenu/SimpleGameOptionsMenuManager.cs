using UnityEngine;
using UnityEngine.Events;

public class SimpleGameOptionsMenuManager : MonoBehaviour
{
    public static SimpleGameOptionsMenuManager Instance { get; private set; }

    [Header("Event")]
    public UnityEvent OnPauseEvents;
    public UnityEvent OnResumeEvents;
    public UnityEvent OnRestartEvents;
    public UnityEvent OnQuitEvents;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] SimpleGameOptionsMenuUIController UIController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        InputManager.Instance.OnEscapeEvent += PauseGame;
    }

    void OnDestroy()
    {
        InputManager.Instance.OnEscapeEvent -= PauseGame;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        OnPauseEvents?.Invoke();
        InputManager.Instance.SetActionMap("UI");
        UIController.OpenMenu();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        OnResumeEvents?.Invoke();
        InputManager.Instance.SetActionMap("Player");
        UIController.CloseMenu();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        OnRestartEvents?.Invoke();
        InputManager.Instance.SetActionMap("Player");
        UIController.CloseMenu();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        OnQuitEvents?.Invoke();
        InputManager.Instance.SetActionMap("Player");
        if (SceneManager.Instance != null) SceneManager.Instance.LoadToScene(mainMenuSceneName);
    }
}
