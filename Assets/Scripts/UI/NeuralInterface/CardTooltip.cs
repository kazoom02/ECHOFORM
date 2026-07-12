using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — CardTooltip
// A floating "what does this chip do?" panel shown while the
// player hovers a chip in the Neural rack. It builds its own
// UI at runtime (canvas + panel + text), so there is NOTHING
// to wire in the Inspector — just call the statics:
//
//   CardTooltip.Show(cardData);   // on pointer enter
//   CardTooltip.Hide();           // on pointer exit
//
// The panel follows the cursor and clamps itself on-screen.
// =====================================================

public class CardTooltip : MonoBehaviour
{
    static CardTooltip instance;

    Canvas canvas;
    RectTransform panel;
    CanvasGroup cg;
    TMP_Text titleText;
    TMP_Text bodyText;
    bool followCursor;

    const float PanelWidth = 300f;
    const float CursorPad  = 18f;

    // ------------------------------------------------------------ public API
    public static void Show(CardData card)
    {
        if (card == null) return;
        Ensure();
        instance.ShowInternal(card, MousePos(), true);
    }

    public static void ShowAt(CardData card, Vector2 screenPosition)
    {
        if (card == null) return;
        Ensure();
        instance.ShowInternal(card, screenPosition, false);
    }

    public static void Hide()
    {
        if (instance != null) instance.HideInternal();
    }

    // ------------------------------------------------------------ bootstrap
    static void Ensure()
    {
        if (instance != null) return;

        var go = new GameObject("CardTooltip (auto)");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CardTooltip>();
        instance.Build();
    }

    void Build()
    {
        // top-level overlay canvas, drawn above everything, ignores clicks
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        // panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(transform, false);
        panel = panelGO.AddComponent<RectTransform>();
        panel.pivot = new Vector2(0f, 1f);              // anchor by top-left corner
        panel.sizeDelta = new Vector2(PanelWidth, 0f);

        var bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
        bg.raycastTarget = false;

        cg = panelGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = MakeText("Title", panel, 24, FontStyles.Bold, new Color(0.55f, 0.9f, 1f));
        bodyText  = MakeText("Body",  panel, 19, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));
    }

    TMP_Text MakeText(string name, Transform parent, float size, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        t.richText = true;
        return t;
    }

    // ------------------------------------------------------------ show / hide
    void ShowInternal(CardData card, Vector2 screenPosition, bool followMouse)
    {
        followCursor = followMouse;

        string cost = card.isGlitch ? "" : $"  <color=#FFD65A>◆{card.energyCost}</color>";
        titleText.text = (string.IsNullOrEmpty(card.cardName) ? "Chip" : card.cardName) + cost;

        bodyText.text = card.isGlitch
            ? "Corrupted memory — unplayable. It clogs your hand and feeds the overload."
            : (string.IsNullOrEmpty(card.description) ? "No effect." : card.description);

        cg.alpha = 1f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);   // size now, so the first frame clamps correctly
        PlaceNear(screenPosition);
    }

    void HideInternal()
    {
        if (cg != null) cg.alpha = 0f;
    }

    void Update()
    {
        if (cg == null || cg.alpha <= 0f) return;
        if (followCursor) PlaceNear(MousePos());
    }

    // place the panel next to a screen position, then nudge it back on-screen
    void PlaceNear(Vector2 screenPosition)
    {
        float w = panel.rect.width;
        float h = panel.rect.height;

        // default: up-right of the position (pivot is top-left)
        float x = screenPosition.x + CursorPad;
        float y = screenPosition.y - CursorPad;

        if (x + w > Screen.width)  x = screenPosition.x - CursorPad - w;   // flip to the left near the right edge
        if (y - h < 0f)            y = screenPosition.y + CursorPad + h;   // flip above near the bottom edge

        x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width  - w));
        y = Mathf.Clamp(y, h, Screen.height);

        panel.position = new Vector3(x, y, 0f);
    }

    static Vector2 MousePos()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
