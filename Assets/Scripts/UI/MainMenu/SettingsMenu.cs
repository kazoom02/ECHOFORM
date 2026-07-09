using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — SettingsMenu
// Basic settings panel: master volume + fullscreen, persisted
// with PlayerPrefs and applied on load. Deliberately small —
// add graphics/quality/controls rows here later as the game
// grows. The Back button should call MainMenuController.ShowMain().
//
// Inspector wiring (all optional — only what you add works):
//   volumeSlider   -> a Slider (0..1)
//   fullscreenToggle -> a Toggle
//   backButton     -> Button that returns to the main panel
// =====================================================

public class SettingsMenu : MonoBehaviour
{
    private const string VolumeKey = "settings_master_volume";
    private const string FullscreenKey = "settings_fullscreen";

    [Header("Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button backButton;

    [Header("Navigation")]
    [SerializeField] private MainMenuController menu;   // for the Back button

    void Awake()
    {
        // Apply saved settings once at startup (safe even if this panel is hidden).
        ApplyVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
        ApplyFullscreen(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);
    }

    void OnEnable()
    {
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, 1f));
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
        if (backButton != null) backButton.onClick.AddListener(OnBack);
    }

    void OnDisable()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);
    }

    private void OnVolumeChanged(float v)
    {
        ApplyVolume(v);
        PlayerPrefs.SetFloat(VolumeKey, v);
    }

    private void OnFullscreenChanged(bool on)
    {
        ApplyFullscreen(on);
        PlayerPrefs.SetInt(FullscreenKey, on ? 1 : 0);
    }

    private void ApplyVolume(float v) => AudioListener.volume = Mathf.Clamp01(v);
    private void ApplyFullscreen(bool on) => Screen.fullScreen = on;

    public void OnBack()
    {
        PlayerPrefs.Save();
        if (menu != null) menu.ShowMain();
    }
}
