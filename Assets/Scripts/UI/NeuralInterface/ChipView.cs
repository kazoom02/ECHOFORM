using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// =====================================================
// ECHOFORM — ChipView
// One memory-chip card sitting in the Neural rack.
// Binds a CardData to the visuals and raises Clicked when
// the player taps it. The glow Image should use the WHITE
// glow sprite (Chip_Glow_White.png) so CardData.tint colors it.
// =====================================================

[DisallowMultipleComponent]
public class ChipView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Bound data")]
    public CardData card;

    [Header("Visuals")]
    [SerializeField] private Image baseImage;    // ChipCard_Base (the frame body)
    [SerializeField] private Image artImage;    // Chip_Icon
    [SerializeField] private Image glowImage;    // Chip_Glow (use the WHITE sprite so tint shows)
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private CanvasGroup group;  // whole-chip fade (optional)

    [Header("Corruption")]
    [SerializeField] private Sprite normalFrame;   // = ChipCard_Base
    [SerializeField] private Sprite glitchFrame;    // = ChipCard_Glitch (shown when card.isGlitch)

    [Header("Controller selection")]
    [SerializeField] private Color controllerHighlightColor = new Color(0.25f, 1f, 1f, 1f);
    [SerializeField] private float controllerHighlightPadding = 6f;
    [SerializeField] private float controllerHighlightThickness = 5f;
    [SerializeField] private float controllerHighlightPulseScale = 1.08f;
    [SerializeField] private float controllerPulseSpeed = 5f;

    public RectTransform Rect => (RectTransform)transform;
    public CanvasGroup Group => group;

    /// <summary>Raised when the player clicks this chip. The HUD listens.</summary>
    public System.Action<ChipView> Clicked;
    public System.Action<ChipView> Hovered;

    RectTransform controllerHighlight;
    Image[] controllerHighlightLines;
    bool controllerSelected;
    bool pointerHovered;

    void Reset() { group = GetComponent<CanvasGroup>(); }

    static readonly Color CorruptRed = new Color(1f, 0.45f, 0.5f);

    public void Bind(CardData c)
    {
        card = c;
        bool glitch = c.isGlitch;
        bool isCopy = glitch && c.art != null;   // a corrupted duplicate keeps the source chip's art

        // Corruption wears the glitch frame; a copy still shows the chip it was cloned from through it.
        if (baseImage && (normalFrame || glitchFrame))
            baseImage.sprite = glitch && glitchFrame ? glitchFrame : normalFrame;

        if (artImage)
        {
            artImage.enabled = c.art != null;                       // copies show source art; generic glitch has none
            artImage.sprite  = c.art;
            artImage.color   = glitch ? CorruptRed : Color.white;   // redden a corrupted copy
        }
        if (glowImage) { glowImage.enabled = !glitch; glowImage.color = c.tint; }
        if (nameLabel) { nameLabel.gameObject.SetActive(!glitch || isCopy); nameLabel.text = c.cardName; }
        if (costLabel) { costLabel.gameObject.SetActive(!glitch); costLabel.text = c.energyCost.ToString(); }
    }

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);

    // Hover: show a tooltip and move the selection rectangle under the mouse.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (card != null) CardTooltip.Show(card);
        SetPointerHovered(true);
        Hovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CardTooltip.Hide();
        SetPointerHovered(false);
    }

    // Make sure the tooltip never lingers if this chip is played/destroyed while hovered.
    void OnDisable()
    {
        CardTooltip.Hide();
        pointerHovered = false;
        UpdateHighlightVisibility();
    }

    void Update()
    {
        if (!IsHighlighted || controllerHighlightLines == null) return;

        float pulse = 0.65f + Mathf.PingPong(Time.unscaledTime * controllerPulseSpeed, 0.35f);
        Color c = controllerHighlightColor;
        c.a *= pulse;
        foreach (Image line in controllerHighlightLines)
            if (line != null) line.color = c;

        if (controllerHighlight != null)
            controllerHighlight.localScale = Vector3.one * Mathf.Lerp(1f, controllerHighlightPulseScale, pulse);
    }

    public void SetControllerSelected(bool selected)
    {
        controllerSelected = selected;
        UpdateHighlightVisibility();
    }

    void SetPointerHovered(bool hovered)
    {
        pointerHovered = hovered;
        UpdateHighlightVisibility();
    }

    bool IsHighlighted => controllerSelected || pointerHovered;

    void UpdateHighlightVisibility()
    {
        if (IsHighlighted)
            EnsureControllerHighlight();

        if (controllerHighlight != null)
            controllerHighlight.gameObject.SetActive(IsHighlighted);
    }

    void EnsureControllerHighlight()
    {
        if (controllerHighlight != null) return;

        GameObject root = new GameObject("ControllerHighlight", typeof(RectTransform));
        RectTransform target = baseImage != null ? baseImage.rectTransform : Rect;
        root.transform.SetParent(target, false);
        root.transform.SetAsLastSibling();
        controllerHighlight = (RectTransform)root.transform;
        controllerHighlight.anchorMin = Vector2.zero;
        controllerHighlight.anchorMax = Vector2.one;
        controllerHighlight.offsetMin = Vector2.one * -controllerHighlightPadding;
        controllerHighlight.offsetMax = Vector2.one * controllerHighlightPadding;
        controllerHighlight.pivot = new Vector2(0.5f, 0.5f);

        controllerHighlightLines = new[]
        {
            CreateHighlightLine("Top"),
            CreateHighlightLine("Bottom"),
            CreateHighlightLine("Left"),
            CreateHighlightLine("Right")
        };

        SetLine(controllerHighlightLines[0], new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, controllerHighlightThickness));
        SetLine(controllerHighlightLines[1], new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, controllerHighlightThickness));
        SetLine(controllerHighlightLines[2], new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(controllerHighlightThickness, 0f));
        SetLine(controllerHighlightLines[3], new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(controllerHighlightThickness, 0f));
    }

    Image CreateHighlightLine(string lineName)
    {
        GameObject line = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        line.transform.SetParent(controllerHighlight, false);
        Image image = line.GetComponent<Image>();
        image.color = controllerHighlightColor;
        image.raycastTarget = false;
        return image;
    }

    static void SetLine(Image line, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        if (line == null) return;

        RectTransform rt = (RectTransform)line.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
    }
}
