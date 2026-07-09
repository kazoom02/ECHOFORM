using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — ScreenSlash
// A full-screen cyan slash streak that sweeps across the
// screen when a memory card activates — the bigger sibling
// of VisorFlash. Put a slash-streak Image over the whole
// Canvas (stretched, anchored center) and assign it; the
// streak sweeps corner-to-corner while flashing in and out.
// Call Slash() to trigger.
//
// No slash sprite? A plain white Image works — set it long
// and thin and this tilts + sweeps it like a blade arc.
// =====================================================

public class ScreenSlash : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform slash;   // the streak image's RectTransform
    [SerializeField] private Image slashImage;      // its Image (for alpha / tint)

    [Header("Look")]
    [SerializeField] private Color color = new Color(0.04f, 1f, 1f);   // cyan
    [SerializeField] private float peakAlpha = 0.85f;
    [Tooltip("Tilt of the streak in degrees.")]
    [SerializeField] private float angle = -25f;

    [Header("Sweep (anchored offsets in px)")]
    [SerializeField] private Vector2 from = new Vector2(-1400f, 400f);
    [SerializeField] private Vector2 to   = new Vector2( 1400f, -400f);

    [Header("Timing")]
    [SerializeField] private float inTime  = 0.06f;   // flash up
    [SerializeField] private float outTime = 0.22f;   // fade out

    void Awake()
    {
        if (slashImage == null && slash != null) slashImage = slash.GetComponent<Image>();
        Hide();
    }

    void Hide()
    {
        if (slashImage) { var c = color; c.a = 0f; slashImage.color = c; }
    }

    public void Slash()
    {
        if (slash == null || slashImage == null) return;
        StopAllCoroutines();
        StartCoroutine(SlashRoutine());
    }

    IEnumerator SlashRoutine()
    {
        slash.localRotation = Quaternion.Euler(0f, 0f, angle);

        Color c = color;
        float total = inTime + outTime;
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / total);
            slash.anchoredPosition = Vector2.LerpUnclamped(from, to, k);   // sweep across

            c.a = t < inTime
                ? Mathf.Lerp(0f, peakAlpha, t / inTime)                    // flash up
                : Mathf.Lerp(peakAlpha, 0f, (t - inTime) / outTime);      // fade out
            slashImage.color = c;
            yield return null;
        }

        c.a = 0f; slashImage.color = c;
    }
}
