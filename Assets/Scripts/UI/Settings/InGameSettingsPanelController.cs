using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — InGameSettingsPanelController
// Abre e fecha as definições durante o jogo, gere a pausa e a navegação
// e confirma a gravação antes do regresso ao menu principal.
// =====================================================

public class InGameSettingsPanelController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private bool openOnStart;
    [SerializeField] private bool pauseWhileOpen = true;
    [SerializeField] private bool selectFirstControlOnOpen = true;
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private Button backButton;

    [Header("Quit to Main Menu")]
    [SerializeField] private Button quitToMenuButton;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private Button confirmSaveButton;
    [SerializeField] private Button cancelQuitButton;
    [SerializeField] private CombatManager combat;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private float timeScaleBeforeOpen = 1f;
    private bool pausedByThisController;
    private bool pausedPlayTimeTracker;

    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;
    private bool IsQuitConfirmationOpen =>
        quitConfirmationPanel != null && quitConfirmationPanel.activeSelf;

    private void Awake()
    {
        if (settingsPanel == null)
        {
            Transform found = transform.Find("SettingsPanel");
            if (found != null) settingsPanel = found.gameObject;
        }

        if (backButton == null && settingsPanel != null)
        {
            Transform found = FindChildRecursive(settingsPanel.transform, "BackButton");
            if (found != null) backButton = found.GetComponent<Button>();
        }

        if (combat == null)
            combat = FindAnyObjectByType<CombatManager>();

        ResolveQuitInterfaceReferences();
    }

    private void Start()
    {
        SetOpen(openOnStart);
    }

    private void OnEnable()
    {
        AddButtonListeners();
    }

    private void Update()
    {
        if (IsQuitConfirmationOpen)
        {
            UiSelectionHelper.RestoreIfMissing(
                quitConfirmationPanel,
                confirmSaveButton != null ? confirmSaveButton.gameObject : null);

            if (TogglePressed() || CancelPressed())
                CloseQuitConfirmation();

            return;
        }

        if (!IsOpen)
            ResumeGameplayIfNeeded();
        else
            UiSelectionHelper.RestoreIfMissing(settingsPanel, firstSelected);

        if (TogglePressed())
            SetOpen(!IsOpen);
        else if (IsOpen && CancelPressed())
            Close();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
        CloseQuitConfirmation(false);
        ResumeGameplayIfNeeded();
    }

    public void Open() => SetOpen(true);

    public void Close() => SetOpen(false);

    public void Toggle() => SetOpen(!IsOpen);

    public void OpenQuitConfirmation()
    {
        if (quitConfirmationPanel == null) return;

        if (!IsOpen)
            SetOpen(true);

        quitConfirmationPanel.SetActive(true);
        quitConfirmationPanel.transform.SetAsLastSibling();

        if (EventSystem.current != null && confirmSaveButton != null)
            EventSystem.current.SetSelectedGameObject(confirmSaveButton.gameObject);
    }

    public void CloseQuitConfirmation()
    {
        CloseQuitConfirmation(true);
    }

    public void SaveAndReturnToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuScene))
        {
            Debug.LogError("Main Menu scene name is empty.");
            return;
        }

        if (combat == null)
            combat = FindAnyObjectByType<CombatManager>();

        bool saved = false;
        try
        {
            saved = combat != null && combat.SaveCurrentProgress();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not write the current Area save: {exception.Message}");
        }

        if (!saved)
        {
            Debug.LogError("Could not save the current Area. The game will remain open to avoid losing progress.");
            return;
        }

        Time.timeScale = 1f;
        pausedByThisController = false;
        pausedPlayTimeTracker = false;

        SceneManager.LoadScene(mainMenuScene);
    }

    private void SetOpen(bool open)
    {
        if (settingsPanel == null) return;
        if (settingsPanel.activeSelf == open)
        {
            if (!open)
            {
                CloseQuitConfirmation(false);
                ResumeGameplayIfNeeded();
            }

            return;
        }

        if (open)
        {
            if (pauseWhileOpen)
            {
                timeScaleBeforeOpen = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedByThisController = true;
            }

            if (PlayTimeTracker.Instance != null && PlayTimeTracker.Instance.IsCounting)
            {
                PlayTimeTracker.Instance.Pause();
                pausedPlayTimeTracker = true;
            }

            settingsPanel.SetActive(true);
            SelectInitialControl();
        }
        else
        {
            CloseQuitConfirmation(false);
            settingsPanel.SetActive(false);
            ResumeGameplayIfNeeded();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void CloseQuitConfirmation(bool restoreSelection)
    {
        if (quitConfirmationPanel != null)
            quitConfirmationPanel.SetActive(false);

        if (restoreSelection && EventSystem.current != null && quitToMenuButton != null)
            EventSystem.current.SetSelectedGameObject(quitToMenuButton.gameObject);
    }

    private void ResumeGameplayIfNeeded()
    {
        if (pausedByThisController)
        {
            Time.timeScale = timeScaleBeforeOpen > 0f ? timeScaleBeforeOpen : 1f;
            pausedByThisController = false;
        }

        if (pausedPlayTimeTracker && PlayTimeTracker.Instance != null)
        {
            PlayTimeTracker.Instance.Resume();
            pausedPlayTimeTracker = false;
        }
    }

    private void SelectInitialControl()
    {
        if (!selectFirstControlOnOpen || EventSystem.current == null) return;

        UiSelectionHelper.SelectFirst(settingsPanel, firstSelected);
    }

    private void AddButtonListeners()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
        }

        if (quitToMenuButton != null)
        {
            quitToMenuButton.onClick.RemoveListener(OpenQuitConfirmation);
            quitToMenuButton.onClick.AddListener(OpenQuitConfirmation);
        }

        if (confirmSaveButton != null)
        {
            confirmSaveButton.onClick.RemoveListener(SaveAndReturnToMainMenu);
            confirmSaveButton.onClick.AddListener(SaveAndReturnToMainMenu);
        }

        if (cancelQuitButton != null)
        {
            cancelQuitButton.onClick.RemoveListener(CloseQuitConfirmation);
            cancelQuitButton.onClick.AddListener(CloseQuitConfirmation);
        }
    }

    private void RemoveButtonListeners()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(Close);

        if (quitToMenuButton != null)
            quitToMenuButton.onClick.RemoveListener(OpenQuitConfirmation);

        if (confirmSaveButton != null)
            confirmSaveButton.onClick.RemoveListener(SaveAndReturnToMainMenu);

        if (cancelQuitButton != null)
            cancelQuitButton.onClick.RemoveListener(CloseQuitConfirmation);
    }

    private void ResolveQuitInterfaceReferences()
    {
        if (settingsPanel == null) return;

        if (quitToMenuButton == null)
        {
            Transform existing = FindChildRecursive(settingsPanel.transform, "QuitToMainMenuButton");
            if (existing != null)
                quitToMenuButton = existing.GetComponent<Button>();
        }

        if (quitConfirmationPanel == null)
        {
            Transform existing = FindChildRecursive(transform, "QuitSaveConfirmation");
            if (existing != null)
                quitConfirmationPanel = existing.gameObject;
        }

        if (confirmSaveButton == null && quitConfirmationPanel != null)
        {
            Transform existing = FindChildRecursive(quitConfirmationPanel.transform, "ConfirmSaveButton");
            if (existing != null) confirmSaveButton = existing.GetComponent<Button>();
        }

        if (cancelQuitButton == null && quitConfirmationPanel != null)
        {
            Transform existing = FindChildRecursive(quitConfirmationPanel.transform, "CancelQuitButton");
            if (existing != null) cancelQuitButton = existing.GetComponent<Button>();
        }

        ConfigureSettingsNavigation();
    }

    private void ConfigureSettingsNavigation()
    {
        Selectable[] controls = settingsPanel.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable control in controls)
            SetAutomaticNavigation(control);
    }

    private static void SetAutomaticNavigation(Selectable selectable)
    {
        if (selectable == null) return;

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        selectable.navigation = navigation;
    }

    private static bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool options = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        return escape || options;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7);
#endif
    }

    private static bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.JoystickButton1);
#endif
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName) return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null) return nested;
        }

        return null;
    }
}
