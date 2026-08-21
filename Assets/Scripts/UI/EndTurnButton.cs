using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — EndTurnButton
// Gere o botão de fim de turno e pede confirmação quando o jogador ainda
// dispõe de ciclos de CPU que seriam desperdiçados.
// =====================================================

[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [SerializeField] private NeuralInterfaceHUD neuralHud;

    [Header("Unused CPU confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmationText;

    Button button;

    bool ConfirmationOpen => confirmationPanel != null && confirmationPanel.activeSelf;

    void Awake()
    {
        button = GetComponent<Button>();
        if (neuralHud == null) neuralHud = FindAnyObjectByType<NeuralInterfaceHUD>();
    }

    void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmEndTurn);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelEndTurn);
        if (combat != null) combat.OnStateChanged += OnStateChanged;
        if (neuralHud == null) neuralHud = FindAnyObjectByType<NeuralInterfaceHUD>();
        if (neuralHud != null) neuralHud.OnBusyChanged += OnHudBusyChanged;
        Refresh();
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmEndTurn);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelEndTurn);
        if (combat != null) combat.OnStateChanged -= OnStateChanged;
        if (neuralHud != null) neuralHud.OnBusyChanged -= OnHudBusyChanged;
        CloseConfirmation(false);
    }

    void OnClick()
    {
        if (ConfirmationOpen || Time.timeScale <= 0f) return;
        if (neuralHud != null && neuralHud.IsBusy) return;
        if (combat == null || combat.State != CombatState.PlayerTurn) return;

        if (combat.Energy > 0)
            OpenConfirmation();
        else
            combat.EndTurn();
    }

    void OpenConfirmation()
    {
        if (confirmationPanel == null)
        {
            Debug.LogError("End Turn confirmation UI is not assigned in the game scene.", this);
            return;
        }

        int pips = combat != null ? combat.Energy : 0;
        if (confirmationText != null)
        {
            string pipWord = pips == 1 ? "pip" : "pips";
            confirmationText.text = $"You still have {pips} CPU {pipWord}.\nEnd the turn anyway?";
        }

        confirmationPanel.SetActive(true);
        confirmationPanel.transform.SetAsLastSibling();
        if (neuralHud != null) neuralHud.SetExternalInputBlocked(true);

        if (EventSystem.current != null && confirmButton != null)
            EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);

        Refresh();
    }

    public void ConfirmEndTurn()
    {
        if (!ConfirmationOpen) return;

        CloseConfirmation(false);
        if (combat != null) combat.EndTurn();
    }

    public void CancelEndTurn()
    {
        CloseConfirmation(true);
    }

    void CloseConfirmation(bool restoreSelection)
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (neuralHud != null) neuralHud.SetExternalInputBlocked(false);

        if (restoreSelection && EventSystem.current != null && button != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);

        Refresh();
    }

    void OnStateChanged(CombatState state)
    {
        if (state != CombatState.PlayerTurn) CloseConfirmation(false);
        Refresh();
    }

    void OnHudBusyChanged(bool busy) => Refresh();

    void Update()
    {
        Refresh();

        if (ConfirmationOpen)
        {
            if (CancelPressed()) CancelEndTurn();
            else if (EndTurnPressed()) ConfirmEndTurn();
            return;
        }

        if (EndTurnPressed()) OnClick();
    }

    void Refresh()
    {
        if (button != null)
            button.interactable = !ConfirmationOpen
                && combat != null
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

    static bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool controller = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        return escape || controller;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
#endif
    }
}
