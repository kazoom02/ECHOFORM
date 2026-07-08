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

    public void Bind(CardData c)
    {
        card = c;

        // Loom corruption: swap to the glitch frame and hide the readable bits.
        if (baseImage && (normalFrame || glitchFrame))
            baseImage.sprite = c.isGlitch && glitchFrame ? glitchFrame : normalFrame;

        if (artImage)  { artImage.enabled = c.art != null && !c.isGlitch; artImage.sprite = c.art; }
        if (glowImage) { glowImage.enabled = !c.isGlitch; glowImage.color = c.tint; } // tint = per-card color
        if (nameLabel) { nameLabel.gameObject.SetActive(!c.isGlitch); nameLabel.text = c.cardName; }
        if (costLabel) { costLabel.gameObject.SetActive(!c.isGlitch); costLabel.text = c.energyCost.ToString(); }
    }

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);
}
