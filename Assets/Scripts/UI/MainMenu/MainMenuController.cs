using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — MainMenuController
// The title screen router.
//   New Game  -> loads Area 1 (the FirstArea scene)
//   Load Game -> opens the load panel (lists saves, or "No games saved")
//   Settings  -> opens the settings panel
//   Quit      -> exits the game
//
// Wire the four buttons and the two panels in the Inspector.
// The root menu buttons live under 'mainPanel' so they can be
// hidden while a sub-panel is open.
// =====================================================

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Scene loaded by New Game. Must be added to Build Settings.")]
    [SerializeField] private string area1SceneName = "FirstArea";

    [Tooltip("Intro video scene shown before Area 1. Leave empty to go straight to Area 1.")]
    [SerializeField] private string introSceneName = "Intro";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;      // holds the four buttons
    [SerializeField] private GameObject loadPanel;      // has a LoadGameMenu
    [SerializeField] private GameObject settingsPanel;  // has a SettingsMenu

    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Sub-menus")]
    [SerializeField] private LoadGameMenu loadGameMenu;
    [SerializeField] private GameObject firstMainSelection;
    [SerializeField] private GameObject firstLoadSelection;
    [SerializeField] private GameObject firstSettingsSelection;

    GameObject activePanel;

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        PlayTimeTracker.EnsureInstance().Pause();
        ResolveReferences();

        AddRuntimeListenerIfNeeded(newGameButton, OnNewGame);
        AddRuntimeListenerIfNeeded(loadGameButton, OnLoadGame);
        AddRuntimeListenerIfNeeded(settingsButton, OnSettings);
        AddRuntimeListenerIfNeeded(quitButton, OnQuit);

        ShowMain();
    }

    void OnDisable()
    {
        if (newGameButton != null)  newGameButton.onClick.RemoveListener(OnNewGame);
        if (loadGameButton != null) loadGameButton.onClick.RemoveListener(OnLoadGame);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
        if (quitButton != null)     quitButton.onClick.RemoveListener(OnQuit);
    }

    void Update()
    {
        UiSelectionHelper.RestoreIfMissing(activePanel, PreferredSelection(activePanel));

        if (CancelPressed() && activePanel != mainPanel)
            ShowMain();
    }

    // ---- Button actions (also assignable from the Inspector OnClick) ----

    public void OnNewGame()
    {
        LoadGameMenu.ClearPendingLoad();
        SaveSystem.DeleteAll();
        PlayTimeTracker.EnsureInstance().BeginNewRun();

        // Play the intro video first if set; otherwise jump straight to Area 1.
        string target = !string.IsNullOrEmpty(introSceneName) ? introSceneName : area1SceneName;
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("[Echoform] MainMenu: no scene set for New Game.");
            return;
        }
        SceneManager.LoadScene(target);
    }

    public void OnLoadGame()
    {
        SetPanel(loadPanel);
        if (loadGameMenu != null) loadGameMenu.Refresh(); // rebuild the list each time it opens
    }

    public void OnSettings() => SetPanel(settingsPanel);

    public void OnQuit()
    {
        Debug.Log("[Echoform] Quit.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---- Panel switching ----

    /// <summary>Called by the Back button on the sub-panels.</summary>
    public void ShowMain() => SetPanel(mainPanel);

    private void SetPanel(GameObject panel)
    {
        if (mainPanel != null)     mainPanel.SetActive(panel == mainPanel);
        if (loadPanel != null)     loadPanel.SetActive(panel == loadPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);

        activePanel = panel;
        UiSelectionHelper.SelectFirst(activePanel, PreferredSelection(activePanel));
    }

    private GameObject PreferredSelection(GameObject panel)
    {
        if (panel == mainPanel) return firstMainSelection != null ? firstMainSelection : ButtonObject(newGameButton);
        if (panel == loadPanel) return firstLoadSelection;
        if (panel == settingsPanel) return firstSettingsSelection;
        return null;
    }

    private void ResolveReferences()
    {
        if (mainPanel == null) mainPanel = FindChild("MainPanel");
        if (loadPanel == null) loadPanel = FindChild("LoadPanel");
        if (settingsPanel == null) settingsPanel = FindChild("SettingsPanel");

        if (newGameButton == null) newGameButton = FindButton("NewGameButton");
        if (loadGameButton == null) loadGameButton = FindButton("LoadGameButton");
        if (settingsButton == null) settingsButton = FindButton("SettingsButton");
        if (quitButton == null) quitButton = FindButton("QuitButton");

        if (loadGameMenu == null && loadPanel != null) loadGameMenu = loadPanel.GetComponentInChildren<LoadGameMenu>(true);

        if (firstMainSelection == null) firstMainSelection = ButtonObject(newGameButton);
        if (firstLoadSelection == null) firstLoadSelection = UiSelectionHelper.FindFirstSelectable(loadPanel);
        if (firstSettingsSelection == null) firstSettingsSelection = UiSelectionHelper.FindFirstSelectable(settingsPanel);
    }

    private GameObject FindChild(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == childName) return child.gameObject;

        Transform[] sceneChildren = FindObjectsOfType<Transform>(true);
        foreach (Transform child in sceneChildren)
            if (child.name == childName) return child.gameObject;

        return null;
    }

    private Button FindButton(string buttonName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == buttonName) return child.GetComponent<Button>();

        Transform[] sceneChildren = FindObjectsOfType<Transform>(true);
        foreach (Transform child in sceneChildren)
            if (child.name == buttonName) return child.GetComponent<Button>();

        return null;
    }

    private static GameObject ButtonObject(Button button)
    {
        return button != null ? button.gameObject : null;
    }

    private static void AddRuntimeListenerIfNeeded(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null) return;
        if (button.onClick.GetPersistentEventCount() > 0) return;

        button.onClick.AddListener(action);
    }

    private static bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;
        return (pad != null && pad.buttonEast.wasPressedThisFrame)
            || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
#endif
    }
}
