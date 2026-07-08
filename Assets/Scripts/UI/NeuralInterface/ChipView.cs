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
public class ChipView : MonoBehaviour, IPointerClickHandler
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

    public RectTransform Rect => (RectTransform)transform;
    public CanvasGroup Group => group;

    /// <summary>Raised when the player clicks this chip. The HUD listens.</summary>
    public System.Action<ChipView> Clicked;

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
}
