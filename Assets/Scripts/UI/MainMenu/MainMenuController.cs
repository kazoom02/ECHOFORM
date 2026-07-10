using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    void OnEnable()
    {
        if (newGameButton != null)  newGameButton.onClick.AddListener(OnNewGame);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        if (quitButton != null)     quitButton.onClick.AddListener(OnQuit);

        ShowMain();
    }

    void OnDisable()
    {
        if (newGameButton != null)  newGameButton.onClick.RemoveListener(OnNewGame);
        if (loadGameButton != null) loadGameButton.onClick.RemoveListener(OnLoadGame);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
        if (quitButton != null)     quitButton.onClick.RemoveListener(OnQuit);
    }

    // ---- Button actions (also assignable from the Inspector OnClick) ----

    public void OnNewGame()
    {
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
    }
}
