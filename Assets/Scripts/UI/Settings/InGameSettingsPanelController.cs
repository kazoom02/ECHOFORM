using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Opens the in-game settings panel from keyboard/controller input.
// Keep this on an always-active object, not on the panel it hides.
public class InGameSettingsPanelController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private bool openOnStart;
    [SerializeField] private bool pauseWhileOpen = true;
    [SerializeField] private bool selectFirstControlOnOpen = true;
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private Button backButton;

    private float timeScaleBeforeOpen = 1f;
    private bool pausedByThisController;
    private bool pausedPlayTimeTracker;

    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

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
    }

    private void Start()
    {
        SetOpen(openOnStart);
    }

    private void OnEnable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
        }
    }

    private void Update()
    {
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
        if (backButton != null)
            backButton.onClick.RemoveListener(Close);

        ResumeGameplayIfNeeded();
    }

    public void Open() => SetOpen(true);

    public void Close() => SetOpen(false);

    public void Toggle() => SetOpen(!IsOpen);

    private void SetOpen(bool open)
    {
        if (settingsPanel == null) return;
        if (settingsPanel.activeSelf == open)
        {
            if (!open) ResumeGameplayIfNeeded();
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
            settingsPanel.SetActive(false);
            ResumeGameplayIfNeeded();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
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
