using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Controller slider highlight")]
    [SerializeField] private Color sliderHighlightColor = new Color(0.25f, 1f, 1f, 1f);
    [SerializeField] private float sliderHighlightPadding = 3f;
    [SerializeField] private float sliderHighlightThickness = 3f;
    [SerializeField] private float sliderHighlightPulseScale = 1.02f;
    [SerializeField] private float sliderHighlightPulseSpeed = 5f;

    private const string KMaster = "audio_master";
    private const string KMusic  = "audio_music";
    private const string KSFX    = "audio_sfx";
    private const string KScenematic = "audio_scenematic";
    private const float  Default = 0.8f;

    RectTransform sliderHighlight;
    Image[] sliderHighlightLines;
    Slider highlightedSlider;
    RectTransform highlightedTarget;

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
        ResolveSliderReferences();
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
        SetSliderHighlight(null);
    }

    void Update()
    {
        Slider selected = FindSelectedAudioSlider();
        SetSliderHighlight(selected);
        PulseSliderHighlight();
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
        ResolveSliderReferences();
        Transform siblings = root.transform.parent;
        if (siblings != null)
            for (int i = 0; i < siblings.childCount; i++)
            {
                Transform c = siblings.GetChild(i);
                if (c != root.transform && c.name.EndsWith("VerticalSlide"))
                    c.gameObject.SetActive(false);
            }
        root.SetActive(true);
        UiSelectionHelper.SelectFirst(root);
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

    private Slider FindSelectedAudioSlider()
    {
        if (root != null && !root.activeInHierarchy) return null;
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return null;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        Slider selected = selectedObject.GetComponentInParent<Slider>();
        if (IsManagedAudioSlider(selected))
            return selected;

        selected = selectedObject.GetComponentInChildren<Slider>(true);
        if (IsManagedAudioSlider(selected))
            return selected;

        return null;
    }

    private void SetSliderHighlight(Slider slider)
    {
        if (slider == null)
        {
            highlightedSlider = null;
            highlightedTarget = null;
            if (sliderHighlight != null) sliderHighlight.gameObject.SetActive(false);
            return;
        }

        RectTransform target = GetSliderHighlightTarget(slider);
        if (target == null)
        {
            highlightedSlider = null;
            highlightedTarget = null;
            if (sliderHighlight != null) sliderHighlight.gameObject.SetActive(false);
            return;
        }

        if (highlightedSlider == slider && highlightedTarget == target) return;

        highlightedSlider = slider;
        highlightedTarget = target;

        EnsureSliderHighlight();
        sliderHighlight.SetParent(target, false);
        sliderHighlight.SetAsLastSibling();
        sliderHighlight.anchorMin = Vector2.zero;
        sliderHighlight.anchorMax = Vector2.one;
        sliderHighlight.offsetMin = Vector2.one * -sliderHighlightPadding;
        sliderHighlight.offsetMax = Vector2.one * sliderHighlightPadding;
        sliderHighlight.pivot = new Vector2(0.5f, 0.5f);
        sliderHighlight.localScale = Vector3.one;
        sliderHighlight.gameObject.SetActive(true);
    }

    private void PulseSliderHighlight()
    {
        if (sliderHighlight == null || !sliderHighlight.gameObject.activeSelf) return;

        float pulse = 0.7f + Mathf.Sin(Time.unscaledTime * sliderHighlightPulseSpeed) * 0.25f;
        Color color = sliderHighlightColor;
        color.a *= pulse;

        foreach (Image line in sliderHighlightLines)
            if (line != null) line.color = color;

        sliderHighlight.localScale = Vector3.one * Mathf.Lerp(1f, sliderHighlightPulseScale, pulse);
    }

    private void ResolveSliderReferences()
    {
        masterSlider = ResolveSliderFromRow(masterSlider, "MasterVolumeRow");
        musicSlider = ResolveSliderFromRow(musicSlider, "MusicVolumeRow");
        sfxSlider = ResolveSliderFromRow(sfxSlider, "SFXVolumeRow");
        Slider resolvedScenematicSlider = ResolveSliderFromRow(
            null,
            "ScenematicVolumeRow",
            "ScenematicsVolumeRow",
            "CinematicVolumeRow",
            "CinematicsVolumeRow");
        scenematicSlider = resolvedScenematicSlider != null || !IsDuplicatePrimarySlider(scenematicSlider)
            ? resolvedScenematicSlider ?? scenematicSlider
            : null;
    }

    private Slider ResolveSliderFromRow(Slider fallback, params string[] rowNames)
    {
        Transform searchRoot = root != null ? root.transform : transform;
        foreach (string rowName in rowNames)
        {
            Transform row = FindChildRecursive(searchRoot, rowName);
            if (row == null) continue;

            Slider slider = row.GetComponentInChildren<Slider>(true);
            if (slider != null) return slider;
        }

        return fallback;
    }

    private bool IsManagedAudioSlider(Slider slider)
    {
        return slider != null &&
               (slider == masterSlider ||
                slider == musicSlider ||
                slider == sfxSlider ||
                slider == scenematicSlider);
    }

    private bool IsDuplicatePrimarySlider(Slider slider)
    {
        return slider != null &&
               (slider == masterSlider ||
                slider == musicSlider ||
                slider == sfxSlider);
    }

    private RectTransform GetSliderHighlightTarget(Slider slider)
    {
        if (slider == null) return null;

        Transform background = FindChildRecursive(slider.transform, "Background");
        if (background is RectTransform backgroundRect)
            return backgroundRect;

        if (slider.fillRect != null && slider.fillRect.parent is RectTransform fillArea)
            return fillArea;

        if (slider.handleRect != null && slider.handleRect.parent is RectTransform handleArea)
            return handleArea;

        return slider.transform as RectTransform;
    }

    private void EnsureSliderHighlight()
    {
        if (sliderHighlight != null) return;

        GameObject rootObject = new GameObject("AudioSliderControllerHighlight", typeof(RectTransform));
        sliderHighlight = (RectTransform)rootObject.transform;
        sliderHighlightLines = new[]
        {
            CreateHighlightLine("Top"),
            CreateHighlightLine("Bottom"),
            CreateHighlightLine("Left"),
            CreateHighlightLine("Right")
        };

        SetLine(sliderHighlightLines[0], new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, sliderHighlightThickness));
        SetLine(sliderHighlightLines[1], new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, sliderHighlightThickness));
        SetLine(sliderHighlightLines[2], new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(sliderHighlightThickness, 0f));
        SetLine(sliderHighlightLines[3], new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(sliderHighlightThickness, 0f));
    }

    private Image CreateHighlightLine(string lineName)
    {
        GameObject line = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        line.transform.SetParent(sliderHighlight, false);
        Image image = line.GetComponent<Image>();
        image.color = sliderHighlightColor;
        image.raycastTarget = false;
        return image;
    }

    private static void SetLine(Image line, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        if (line == null) return;

        RectTransform rect = line.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
    }

    private static Transform FindChildRecursive(Transform current, string targetName)
    {
        if (current == null || string.IsNullOrEmpty(targetName)) return null;
        if (current.name == targetName) return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindChildRecursive(current.GetChild(i), targetName);
            if (found != null) return found;
        }

        return null;
    }

    private static string Pct(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";

    private static void Save(string key, float v)
    {
        PlayerPrefs.SetFloat(key, v);
        PlayerPrefs.Save();
    }
}
