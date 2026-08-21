using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — SettingsMenu
// Controla as opções simples de volume geral e ecrã inteiro do menu
// principal e guarda os valores nas preferências do jogador.
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
    [SerializeField] private MainMenuController menu;

    void Awake()
    {

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

        UiSelectionHelper.SelectFirst(gameObject, volumeSlider != null ? volumeSlider.gameObject : null);
    }

    void OnDisable()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);
    }

    void Update()
    {
        UiSelectionHelper.RestoreIfMissing(gameObject, volumeSlider != null ? volumeSlider.gameObject : null);
        if (CancelPressed()) OnBack();
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
