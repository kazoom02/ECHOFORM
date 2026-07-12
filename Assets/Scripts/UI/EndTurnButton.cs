using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — EndTurnButton
// Ends the player's turn (enemies then act). Auto-disables
// while it isn't the player's turn so you can't spam it or
// act during the enemy phase. Put on a UI Button.
// =====================================================

[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [SerializeField] private NeuralInterfaceHUD neuralHud;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (neuralHud == null) neuralHud = FindObjectOfType<NeuralInterfaceHUD>();
    }

    void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        if (combat != null) combat.OnStateChanged += OnStateChanged;
        if (neuralHud == null) neuralHud = FindObjectOfType<NeuralInterfaceHUD>();
        if (neuralHud != null) neuralHud.OnBusyChanged += OnHudBusyChanged;
        Refresh();
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        if (combat != null) combat.OnStateChanged -= OnStateChanged;
        if (neuralHud != null) neuralHud.OnBusyChanged -= OnHudBusyChanged;
    }

    void OnClick()
    {
        if (Time.timeScale <= 0f) return;
        if (neuralHud != null && neuralHud.IsBusy) return;
        if (combat != null) combat.EndTurn();
    }

    void OnStateChanged(CombatState state) => Refresh();
    void OnHudBusyChanged(bool busy) => Refresh();

    void Update()
    {
        Refresh();

        if (EndTurnPressed())
            OnClick();
    }

    void Refresh()
    {
        if (button != null)
            button.interactable = combat != null
                && combat.State == CombatState.PlayerTurn
                && Time.timeScale > 0f
                && (neuralHud == null || !neuralHud.IsBusy);
    }

    static bool EndTurnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        return pad != null && (pad.buttonNorth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.JoystickButton5);
#endif
    }
}
