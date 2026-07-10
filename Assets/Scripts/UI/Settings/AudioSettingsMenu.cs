using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

// =====================================================
// ECHOFORM — AudioSettingsMenu
// Master / Music / SFX / Scenematics volume sliders. Applies live (no Apply/Revert),
// persists to PlayerPrefs, and reapplies on launch. Drives an AudioMixer
// through exposed parameters (MasterVol / MusicVol / SFXVol / ScenematicsVol). If no mixer
// is assigned, Master falls back to AudioListener.volume so it still works.
//
// Build it with Tools > ECHOFORM > Build Audio Settings, or wire the
// sliders/root/mixer by hand in the Inspector.
// =====================================================

public class AudioSettingsMenu : MonoBehaviour
{
    [Header("Root (a *VerticalSlide shown for the Audio tab)")]
    [SerializeField] private GameObject root;

    [Tooltip("Show the Audio panel automatically whenever the Settings screen opens.")]
    [SerializeField] private bool openByDefault = false;

    [Header("Mixer (exposed params below)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam  = "MusicVol";
    [SerializeField] private string sfxParam    = "SFXVol";
    [SerializeField] private string scenematicParam = "ScenematicsVol";

    [Header("Sliders (0..1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider scenematicSlider;

    [Header("Value labels (optional)")]
    [SerializeField] private TMP_Text masterValue;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text sfxValue;
    [SerializeField] private TMP_Text scenematicValue;

    private const string KMaster = "audio_master";
    private const string KMusic  = "audio_music";
    private const string KSFX    = "audio_sfx";
    private const string KScenematic = "audio_scenematic";
    private const float  Default = 0.8f;

    void Awake()
    {
        // Apply saved levels at startup even if this panel is hidden.
        ApplyMaster(PlayerPrefs.GetFloat(KMaster, Default));
        ApplyMusic (PlayerPrefs.GetFloat(KMusic,  Default));
        ApplySFX   (PlayerPrefs.GetFloat(KSFX,    Default));
        ApplyScenematic(PlayerPrefs.GetFloat(KScenematic, Default));

        if (root != null) root.SetActive(false);
    }

    void OnEnable()
    {
        Bind(masterSlider, KMaster, OnMaster);
        Bind(musicSlider,  KMusic,  OnMusic);
        Bind(sfxSlider,    KSFX,    OnSFX);
        Bind(scenematicSlider, KScenematic, OnScenematic);

        RefreshLabels();
        if (openByDefault) Show();
    }

    void OnDisable()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMaster);
        if (musicSlider  != null) musicSlider.onValueChanged.RemoveListener(OnMusic);
        if (sfxSlider    != null) sfxSlider.onValueChanged.RemoveListener(OnSFX);
        if (scenematicSlider != null) scenematicSlider.onValueChanged.RemoveListener(OnScenematic);
    }

    private void Bind(Slider s, string key, UnityEngine.Events.UnityAction<float> cb)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, Default));
        s.onValueChanged.RemoveListener(cb);
        s.onValueChanged.AddListener(cb);
    }

    // Shows this slide and hides any sibling *VerticalSlide (tab exclusivity).
    public void Show()
    {
        if (root == null) return;
        Transform siblings = root.transform.parent;
        if (siblings != null)
            for (int i = 0; i < siblings.childCount; i++)
            {
                Transform c = siblings.GetChild(i);
                if (c != root.transform && c.name.EndsWith("VerticalSlide"))
                    c.gameObject.SetActive(false);
            }
        root.SetActive(true);
    }

    public void Hide() { if (root != null) root.SetActive(false); }

    private void OnMaster(float v) { ApplyMaster(v); Save(KMaster, v); if (masterValue) masterValue.text = Pct(v); }
    private void OnMusic (float v) { ApplyMusic(v);  Save(KMusic,  v); if (musicValue)  musicValue.text  = Pct(v); }
    private void OnSFX   (float v) { ApplySFX(v);    Save(KSFX,    v); if (sfxValue)    sfxValue.text    = Pct(v); }
    private void OnScenematic(float v) { ApplyScenematic(v); Save(KScenematic, v); if (scenematicValue) scenematicValue.text = Pct(v); }

    private void ApplyMaster(float v) { if (!SetMixer(masterParam, v)) AudioListener.volume = Mathf.Clamp01(v); }
    private void ApplyMusic (float v) { SetMixer(musicParam, v); }
    private void ApplySFX   (float v) { SetMixer(sfxParam,   v); }
    private void ApplyScenematic(float v) { SetMixer(scenematicParam, v); }

    // 0..1 -> decibels; 0 maps to -80 dB (effectively silent).
    private bool SetMixer(string param, float v01)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return false;
        float dB = v01 <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(v01)) * 20f;
        return mixer.SetFloat(param, dB);
    }

    private void RefreshLabels()
    {
        if (masterValue) masterValue.text = Pct(PlayerPrefs.GetFloat(KMaster, Default));
        if (musicValue)  musicValue.text  = Pct(PlayerPrefs.GetFloat(KMusic,  Default));
        if (sfxValue)    sfxValue.text     = Pct(PlayerPrefs.GetFloat(KSFX,    Default));
        if (scenematicValue) scenematicValue.text = Pct(PlayerPrefs.GetFloat(KScenematic, Default));
    }

    private static string Pct(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";

    private static void Save(string key, float v)
    {
        PlayerPrefs.SetFloat(key, v);
        PlayerPrefs.Save();
    }
}
