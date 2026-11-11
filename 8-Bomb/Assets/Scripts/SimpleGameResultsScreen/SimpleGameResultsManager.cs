using UnityEngine;
using UnityEngine.Events;

public class SimpleGameResultsManager : MonoBehaviour
{
    public static SimpleGameResultsManager Instance { get; private set; }

    [Header("Event")]
    public UnityEvent OnLevelCompletedEvents;
    public UnityEvent OnLevelFailedEvents;
    public UnityEvent OnRestartLevelEvents;
    public UnityEvent OnNextLevelEvents;
    public UnityEvent OnQuitEvents;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] SimpleGameResultsScreenUIController UIController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowResultsScreen(string msg, bool isLevelCompleted = false)
    {
        Time.timeScale = 0f;
        if (isLevelCompleted)
        {
            OnLevelCompletedEvents?.Invoke();
        }
        else
        {
            OnLevelFailedEvents?.Invoke();
        }
        InputManager.Instance.SetActionMap("UI");
        UIController.ShowResultsScreen(msg);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        OnRestartLevelEvents?.Invoke();
        InputManager.Instance.SetActionMap("Player");
        UIController.CloseResultsScreen();
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        OnNextLevelEvents?.Invoke();
        InputManager.Instance.SetActionMap("Player");
        UIController.CloseResultsScreen();
    }

    public void QuitLevel()
    {
        Time.timeScale = 1f;
        OnQuitEvents?.Invoke();
        InputManager.Instance.SetActionMap("Player");
        if (SceneManager.Instance != null) SceneManager.Instance.LoadToScene(mainMenuSceneName);
    }
}
