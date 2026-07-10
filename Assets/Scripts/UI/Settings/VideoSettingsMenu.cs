using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — VideoSettingsMenu
// Video options: Display, Resolution, Window Mode, Refresh Rate.
// Values are read from the actual system. Changes stage until
// the player presses Apply; after applying, a 15s countdown asks
// them to Keep the change or it auto-reverts (protects against a
// resolution/mode that blackscreens the display). Kept settings
// persist via PlayerPrefs and re-apply on next launch.
//
// Wire it up with Tools > ECHOFORM > Build Video Settings, or
// assign the dropdowns/buttons/root by hand in the Inspector.
// =====================================================

public class VideoSettingsMenu : MonoBehaviour
{
    [Header("Root (shown/hidden by the Video button)")]
    [SerializeField] private GameObject root;

    [Tooltip("Show the Video panel automatically whenever the Settings screen opens (ContentArea is enabled).")]
    [SerializeField] private bool openByDefault = true;

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown displayDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private TMP_Dropdown refreshRateDropdown;

    [Header("Apply / Revert")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button revertButton;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private float revertSeconds = 15f;

    // PlayerPrefs keys
    private const string KW = "video_res_w";
    private const string KH = "video_res_h";
    private const string KRN = "video_refresh_num";
    private const string KRD = "video_refresh_den";
    private const string KMode = "video_mode";
    private const string KDisplay = "video_display";

    private struct VideoState
    {
        public int width, height;
        public RefreshRate refresh;
        public FullScreenMode mode;
        public int display;
    }

    private static readonly FullScreenMode[] Modes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed,
    };
    private static readonly string[] ModeLabels = { "Fullscreen", "Borderless", "Windowed" };

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private readonly List<List<RefreshRate>> refreshByRes = new List<List<RefreshRate>>();
    private readonly List<DisplayInfo> displays = new List<DisplayInfo>();

    private VideoState confirmed;      // last kept state (revert target)
    private bool awaitingConfirm;
    private Coroutine countdown;

    // ---------------------------------------------------------------

    void Awake()
    {
        LoadAndApplySaved();

        if (applyButton != null)  applyButton.onClick.AddListener(OnApplyOrKeep);
        if (revertButton != null) revertButton.onClick.AddListener(OnRevertOrReset);

        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (root != null) root.SetActive(false);
    }

    // When the Settings screen opens, ContentArea is enabled and this fires —
    // default the panel to the Video slide so it shows without a click.
    void OnEnable()
    {
        if (openByDefault) Show();
    }

    // Leaving the panel (Back button, tab switch that disables ContentArea, or
    // scene change) discards anything that wasn't kept: an applied-but-unconfirmed
    // resolution is reverted to the last kept state, and un-applied dropdown edits
    // are reset to what's actually active.
    void OnDisable()
    {
        if (awaitingConfirm) Revert();
        else SyncDropdownsToCurrent();
    }

    void OnDestroy()
    {
        if (applyButton != null)  applyButton.onClick.RemoveListener(OnApplyOrKeep);
        if (revertButton != null) revertButton.onClick.RemoveListener(OnRevertOrReset);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
    }

    // Hooked to VideoSettingsButton.onClick.
    public void Show()
    {
        RebuildOptions();
        SyncDropdownsToCurrent();
        EndConfirm();

        if (root == null) return;

        // Tab exclusivity: hide any sibling *VerticalSlide (e.g. Audio) so only
        // one settings panel shows at a time inside ContentArea.
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

    public void Hide()
    {
        if (awaitingConfirm) Revert();   // don't leave an unconfirmed change hanging
        if (root != null) root.SetActive(false);
    }

    // ---------------------------------------------------------------
    // Option lists

    private void RebuildOptions()
    {
        // Resolutions (distinct width x height) + refresh rates per resolution.
        resolutions.Clear();
        refreshByRes.Clear();
        foreach (Resolution r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            int idx = resolutions.IndexOf(size);
            if (idx < 0)
            {
                resolutions.Add(size);
                refreshByRes.Add(new List<RefreshRate>());
                idx = resolutions.Count - 1;
            }
            if (!ContainsRate(refreshByRes[idx], r.refreshRateRatio))
                refreshByRes[idx].Add(r.refreshRateRatio);
        }
        if (resolutions.Count == 0)
        {
            resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            refreshByRes.Add(new List<RefreshRate> { Screen.currentResolution.refreshRateRatio });
        }

        // Displays (monitors) via the windowing layout API.
        displays.Clear();
        Screen.GetDisplayLayout(displays);
        if (displays.Count == 0) displays.Add(Screen.mainWindowDisplayInfo);

        // Fill dropdowns
        if (displayDropdown != null)
        {
            var opts = new List<string>();
            for (int i = 0; i < displays.Count; i++)
                opts.Add(string.IsNullOrEmpty(displays[i].name) ? $"Display {i + 1}" : displays[i].name);
            SetOptions(displayDropdown, opts);
            displayDropdown.interactable = displays.Count > 1;
        }

        if (resolutionDropdown != null)
        {
            var opts = new List<string>();
            foreach (Vector2Int s in resolutions) opts.Add($"{s.x} x {s.y}");
            SetOptions(resolutionDropdown, opts);
        }

        if (windowModeDropdown != null)
            SetOptions(windowModeDropdown, new List<string>(ModeLabels));

        // Refresh rate list depends on the selected resolution; filled in RebuildRefreshRates.
    }

    private void RebuildRefreshRates(int resIndex)
    {
        if (refreshRateDropdown == null) return;
        var opts = new List<string>();
        if (resIndex >= 0 && resIndex < refreshByRes.Count)
            foreach (RefreshRate rr in refreshByRes[resIndex])
                opts.Add($"{Mathf.RoundToInt((float)rr.value)} Hz");
        if (opts.Count == 0) opts.Add("—");
        SetOptions(refreshRateDropdown, opts);
    }

    private void OnResolutionChanged(int resIndex)
    {
        RebuildRefreshRates(resIndex);
    }

    // ---------------------------------------------------------------
    // Reading current state into the dropdowns

    private void SyncDropdownsToCurrent()
    {
        VideoState cur = ReadCurrentState();
        confirmed = cur;

        int resIdx = resolutions.IndexOf(new Vector2Int(cur.width, cur.height));
        if (resIdx < 0) resIdx = resolutions.Count - 1;
        if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(resIdx);

        RebuildRefreshRates(resIdx);
        if (refreshRateDropdown != null)
            refreshRateDropdown.SetValueWithoutNotify(RateIndex(resIdx, cur.refresh));

        if (windowModeDropdown != null)
            windowModeDropdown.SetValueWithoutNotify(Mathf.Max(0, System.Array.IndexOf(Modes, cur.mode)));

        if (displayDropdown != null)
            displayDropdown.SetValueWithoutNotify(Mathf.Clamp(cur.display, 0, Mathf.Max(0, displays.Count - 1)));
    }

    private VideoState ReadCurrentState()
    {
        DisplayInfo main = Screen.mainWindowDisplayInfo;
        int disp = 0;
        for (int i = 0; i < displays.Count; i++)
            if (displays[i].name == main.name) { disp = i; break; }

        return new VideoState
        {
            width = Screen.width,
            height = Screen.height,
            refresh = Screen.currentResolution.refreshRateRatio,
            mode = Screen.fullScreenMode,
            display = disp,
        };
    }

    private VideoState ReadDropdownState()
    {
        int resIdx = resolutionDropdown != null ? resolutionDropdown.value : 0;
        resIdx = Mathf.Clamp(resIdx, 0, Mathf.Max(0, resolutions.Count - 1));
        Vector2Int size = resolutions[resIdx];

        RefreshRate rr = Screen.currentResolution.refreshRateRatio;
        if (refreshRateDropdown != null && resIdx < refreshByRes.Count)
        {
            int rIdx = Mathf.Clamp(refreshRateDropdown.value, 0, Mathf.Max(0, refreshByRes[resIdx].Count - 1));
            if (refreshByRes[resIdx].Count > 0) rr = refreshByRes[resIdx][rIdx];
        }

        int modeIdx = windowModeDropdown != null ? Mathf.Clamp(windowModeDropdown.value, 0, Modes.Length - 1) : 0;
        int disp = displayDropdown != null ? Mathf.Clamp(displayDropdown.value, 0, Mathf.Max(0, displays.Count - 1)) : 0;

        return new VideoState
        {
            width = size.x,
            height = size.y,
            refresh = rr,
            mode = Modes[modeIdx],
            display = disp,
        };
    }

    // ---------------------------------------------------------------
    // Apply / Keep / Revert

    private void OnApplyOrKeep()
    {
        if (awaitingConfirm) Keep();
        else Apply();
    }

    private void OnRevertOrReset()
    {
        if (awaitingConfirm) Revert();
        else SyncDropdownsToCurrent();   // discard un-applied edits
    }

    private void Apply()
    {
        VideoState target = ReadDropdownState();
        ApplyState(target);
        BeginConfirm();
    }

    private void Keep()
    {
        confirmed = ReadDropdownState();
        Save(confirmed);
        EndConfirm();
        if (statusLabel != null) statusLabel.text = "Saved.";
    }

    private void Revert()
    {
        ApplyState(confirmed);
        EndConfirm();
        SyncDropdownsToCurrent();
    }

    private void ApplyState(VideoState s)
    {
        if (displays.Count > 1 && s.display >= 0 && s.display < displays.Count)
            Screen.MoveMainWindowTo(displays[s.display], Vector2Int.zero);

        Screen.SetResolution(s.width, s.height, s.mode, s.refresh);
    }

    private void BeginConfirm()
    {
        awaitingConfirm = true;
        SetButtonLabel(applyButton, "Keep");
        SetButtonLabel(revertButton, "Revert");
        if (countdown != null) StopCoroutine(countdown);
        countdown = StartCoroutine(CountdownRoutine());
    }

    private void EndConfirm()
    {
        awaitingConfirm = false;
        if (countdown != null) { StopCoroutine(countdown); countdown = null; }
        SetButtonLabel(applyButton, "Apply");
        SetButtonLabel(revertButton, "Revert");
        if (statusLabel != null) statusLabel.text = string.Empty;
    }

    private IEnumerator CountdownRoutine()
    {
        float t = revertSeconds;
        while (t > 0f)
        {
            if (statusLabel != null)
                statusLabel.text = $"Keep these settings? Reverting in {Mathf.CeilToInt(t)}s";
            yield return new WaitForSecondsRealtime(1f);
            t -= 1f;
        }
        Revert();
    }

    // ---------------------------------------------------------------
    // Persistence

    private void Save(VideoState s)
    {
        PlayerPrefs.SetInt(KW, s.width);
        PlayerPrefs.SetInt(KH, s.height);
        PlayerPrefs.SetInt(KRN, (int)s.refresh.numerator);
        PlayerPrefs.SetInt(KRD, (int)s.refresh.denominator);
        PlayerPrefs.SetInt(KMode, (int)s.mode);
        PlayerPrefs.SetInt(KDisplay, s.display);
        PlayerPrefs.Save();
    }

    private void LoadAndApplySaved()
    {
        if (!PlayerPrefs.HasKey(KW)) return;

        var s = new VideoState
        {
            width = PlayerPrefs.GetInt(KW, Screen.width),
            height = PlayerPrefs.GetInt(KH, Screen.height),
            mode = (FullScreenMode)PlayerPrefs.GetInt(KMode, (int)Screen.fullScreenMode),
            display = PlayerPrefs.GetInt(KDisplay, 0),
        };
        uint num = (uint)PlayerPrefs.GetInt(KRN, 0);
        uint den = (uint)PlayerPrefs.GetInt(KRD, 0);
        s.refresh = den > 0
            ? new RefreshRate { numerator = num, denominator = den }
            : Screen.currentResolution.refreshRateRatio;

        // Displays may not be enumerated yet; MoveMainWindowTo is best-effort.
        Screen.SetResolution(s.width, s.height, s.mode, s.refresh);
    }

    // ---------------------------------------------------------------
    // Small helpers

    private static void SetOptions(TMP_Dropdown dd, List<string> options)
    {
        dd.ClearOptions();
        dd.AddOptions(options);
        dd.RefreshShownValue();
    }

    private static bool ContainsRate(List<RefreshRate> list, RefreshRate rr)
    {
        foreach (RefreshRate r in list)
            if (Mathf.RoundToInt((float)r.value) == Mathf.RoundToInt((float)rr.value)) return true;
        return false;
    }

    private int RateIndex(int resIdx, RefreshRate rr)
    {
        if (resIdx < 0 || resIdx >= refreshByRes.Count) return 0;
        List<RefreshRate> list = refreshByRes[resIdx];
        for (int i = 0; i < list.Count; i++)
            if (Mathf.RoundToInt((float)list[i].value) == Mathf.RoundToInt((float)rr.value)) return i;
        return Mathf.Max(0, list.Count - 1);
    }

    private static void SetButtonLabel(Button b, string text)
    {
        if (b == null) return;
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = text;
    }
}
