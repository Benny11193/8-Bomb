using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    // Input utility
    public event Action OnEscapeEvent;
    public event Action<bool> OnPressEvent;
    public event Action<Vector2> OnMoveEvent;
    public event Action OnSwitchEvent;

    // private
    private PlayerInput playerInput;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);

        playerInput = GetComponent<PlayerInput>();
    }

    public void SetActionMap(string mapName)
    {
        // Find the target map by name
        InputActionMap targetMap = playerInput.actions.FindActionMap(mapName);

        if (targetMap == null)
        {
            Debug.LogError($"Action Map named '{mapName}' not found in the Input Actions asset! Please check spelling in the Inspector.");
            return;
        }

        // Disable all other maps
        foreach (var am in playerInput.actions.actionMaps)
        {
            if (am.enabled)
            {
                am.Disable();
            }
        }

        // Enable the selected map
        targetMap.Enable();

        Debug.Log($"Initial Input Map set to: {mapName}");
    }

    void OnEscape(InputValue input)
    {
        OnEscapeEvent?.Invoke();
    }

    void OnPress(InputValue input)
    {
        OnPressEvent?.Invoke(input.isPressed);
    }

    void OnMove(InputValue input)
    {
        Vector2 moveInput = input.Get<Vector2>();
        OnMoveEvent?.Invoke(moveInput);
    }

    void OnSwitch(InputValue input)
    {
        OnSwitchEvent?.Invoke();
    }
}
