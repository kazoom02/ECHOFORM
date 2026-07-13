using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
#endif

public enum ControlsDeviceFamily
{
    KeyboardMouse,
    Xbox,
    PlayStation
}

// Tracks the last device that produced a button press even while the controls
// page is hidden, so opening Settings with Escape/Options selects the right UI.
public static class ControlsDeviceTracker
{
    public static ControlsDeviceFamily Current { get; private set; } = ControlsDeviceFamily.KeyboardMouse;
    public static event Action<ControlsDeviceFamily> Changed;

#if ENABLE_INPUT_SYSTEM
    private static IDisposable buttonListener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        buttonListener?.Dispose();
        buttonListener = InputSystem.onAnyButtonPress.Call(control => MarkDevice(control.device));
    }

    public static void MarkDevice(InputDevice device)
    {
        if (device is Keyboard || device is Mouse)
            Set(ControlsDeviceFamily.KeyboardMouse);
        else if (device is Gamepad pad)
            Set(DetectGamepadFamily(pad));
    }

    private static ControlsDeviceFamily DetectGamepadFamily(Gamepad pad)
    {
        string description = $"{pad.layout} {pad.name} {pad.displayName} " +
                             $"{pad.description.manufacturer} {pad.description.product} " +
                             $"{pad.description.interfaceName}";
        description = description.ToLowerInvariant();

        if (description.Contains("xbox") || description.Contains("xinput") || description.Contains("microsoft"))
            return ControlsDeviceFamily.Xbox;

        if (description.Contains("dualsense") || description.Contains("dualshock") ||
            description.Contains("playstation") || description.Contains("sony") ||
            description.Contains("wireless controller"))
            return ControlsDeviceFamily.PlayStation;

        // Most generic PC gamepads expose the same south/east/north layout as Xbox.
        return ControlsDeviceFamily.Xbox;
    }
#endif

    private static void Set(ControlsDeviceFamily family)
    {
        if (Current == family) return;
        Current = family;
        Changed?.Invoke(family);
    }
}

[RequireComponent(typeof(ScrollRect))]
public class ControlsSettingsDisplay : MonoBehaviour
{
    private struct ControlRow
    {
        public readonly string action;
        public readonly string binding;

        public ControlRow(string action, string binding)
        {
            this.action = action;
            this.binding = binding;
        }
    }

    private RectTransform content;
    private TMP_Text styleTemplate;
    private TMP_Text deviceValue;
    private TMP_Text[] bindingValues;
    private bool built;

#if ENABLE_INPUT_SYSTEM
    private Vector4 previousPadAxes;
#endif

    private static readonly string[] ActionNames =
    {
        "Navigate / Choose",
        "Select Target",
        "Confirm / Play Card",
        "Cancel / Back",
        "End Turn",
        "Open Settings"
    };

    private void Awake()
    {
        ScrollRect scroll = GetComponent<ScrollRect>();
        content = scroll != null ? scroll.content : null;
        Build();
    }

    private void OnEnable()
    {
        Build();
        ControlsDeviceTracker.Changed += Refresh;
        Refresh(ControlsDeviceTracker.Current);

#if ENABLE_INPUT_SYSTEM
        previousPadAxes = ReadPadAxes(Gamepad.current);
#endif
    }

    private void OnDisable()
    {
        ControlsDeviceTracker.Changed -= Refresh;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.scroll.ReadValue().sqrMagnitude > 0.01f))
            ControlsDeviceTracker.MarkDevice(mouse);

        Gamepad pad = Gamepad.current;
        Vector4 axes = ReadPadAxes(pad);
        if (pad != null && (axes - previousPadAxes).sqrMagnitude > 0.01f && axes.sqrMagnitude > 0.09f)
            ControlsDeviceTracker.MarkDevice(pad);
        previousPadAxes = axes;
#endif
    }

    private void Build()
    {
        if (built || content == null) return;

        styleTemplate = content.GetComponentInChildren<TMP_Text>(true);

        // The scene originally contained copied video-setting rows. Preserve
        // them in the scene file but hide them at runtime for this controls page.
        for (int i = 0; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        bindingValues = new TMP_Text[ActionNames.Length];
        CreateRow("ACTIVE INPUT", string.Empty, true, out deviceValue);

        for (int i = 0; i < ActionNames.Length; i++)
            CreateRow(ActionNames[i], string.Empty, false, out bindingValues[i]);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        built = true;
    }

    private void Refresh(ControlsDeviceFamily family)
    {
        if (!built) return;

        ControlRow[] rows = RowsFor(family);
        deviceValue.text = family switch
        {
            ControlsDeviceFamily.Xbox => "XBOX CONTROLLER",
            ControlsDeviceFamily.PlayStation => "PLAYSTATION CONTROLLER",
            _ => "KEYBOARD + MOUSE"
        };

        for (int i = 0; i < bindingValues.Length && i < rows.Length; i++)
            bindingValues[i].text = rows[i].binding;
    }

    private static ControlRow[] RowsFor(ControlsDeviceFamily family)
    {
        if (family == ControlsDeviceFamily.PlayStation)
        {
            return new[]
            {
                new ControlRow(ActionNames[0], "Left Stick / D-Pad"),
                new ControlRow(ActionNames[1], "Left Stick / D-Pad"),
                new ControlRow(ActionNames[2], "Cross"),
                new ControlRow(ActionNames[3], "Circle"),
                new ControlRow(ActionNames[4], "Triangle / R1"),
                new ControlRow(ActionNames[5], "Options")
            };
        }

        if (family == ControlsDeviceFamily.Xbox)
        {
            return new[]
            {
                new ControlRow(ActionNames[0], "Left Stick / D-Pad"),
                new ControlRow(ActionNames[1], "Left Stick / D-Pad"),
                new ControlRow(ActionNames[2], "A"),
                new ControlRow(ActionNames[3], "B"),
                new ControlRow(ActionNames[4], "Y / RB"),
                new ControlRow(ActionNames[5], "Menu")
            };
        }

        return new[]
        {
            new ControlRow(ActionNames[0], "Mouse / Arrow Keys"),
            new ControlRow(ActionNames[1], "Mouse / Arrow Keys"),
            new ControlRow(ActionNames[2], "Left Click / Enter / Space"),
            new ControlRow(ActionNames[3], "Right Click / Esc"),
            new ControlRow(ActionNames[4], "Click END TURN"),
            new ControlRow(ActionNames[5], "Esc")
        };
    }

    private void CreateRow(string action, string binding, bool header, out TMP_Text bindingText)
    {
        GameObject row = new GameObject(header ? "ActiveInputRow" : action.Replace(" / ", string.Empty) + "Row",
                                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        row.layer = content.gameObject.layer;
        row.transform.SetParent(content, false);

        Image background = row.GetComponent<Image>();
        background.raycastTarget = false;
        background.color = header
            ? new Color(0.08f, 0.32f, 0.42f, 0.75f)
            : new Color(0.02f, 0.08f, 0.12f, 0.52f);

        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.preferredHeight = header ? 7f : 6f;

        TMP_Text actionText = CreateText("Action", row.transform, new Vector2(0f, 0f), new Vector2(0.48f, 1f));
        actionText.text = action;
        actionText.alignment = TextAlignmentOptions.MidlineLeft;
        actionText.color = header ? new Color(0.65f, 0.93f, 1f) : Color.white;

        bindingText = CreateText("Binding", row.transform, new Vector2(0.48f, 0f), new Vector2(1f, 1f));
        bindingText.text = binding;
        bindingText.alignment = TextAlignmentOptions.MidlineRight;
        bindingText.color = new Color(0.35f, 0.82f, 1f);
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = content.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(2.2f, 0.4f);
        rect.offsetMax = new Vector2(-2.2f, -0.4f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = 1.45f;
        text.fontSizeMax = 2.85f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        if (styleTemplate != null)
        {
            text.font = styleTemplate.font;
            text.fontSharedMaterial = styleTemplate.fontSharedMaterial;
        }

        return text;
    }

#if ENABLE_INPUT_SYSTEM
    private static Vector4 ReadPadAxes(Gamepad pad)
    {
        if (pad == null) return Vector4.zero;
        Vector2 left = pad.leftStick.ReadValue();
        Vector2 right = pad.rightStick.ReadValue();
        return new Vector4(left.x, left.y, right.x, right.y);
    }
#endif
}
