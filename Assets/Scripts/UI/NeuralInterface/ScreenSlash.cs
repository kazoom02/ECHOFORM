using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — ScreenSlash
// Anima um golpe luminoso que atravessa o ecrã quando uma carta de ataque
// é executada, controlando a posição, inclinação, cor e transparência.
// =====================================================

public class ScreenSlash : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform slash;
    [SerializeField] private Image slashImage;

    [Header("Look")]
    [SerializeField] private Color color = new Color(0.04f, 1f, 1f);
    [SerializeField] private float peakAlpha = 0.85f;
    [Tooltip("Tilt of the streak in degrees.")]
    [SerializeField] private float angle = -25f;

    [Header("Sweep (anchored offsets in px)")]
    [SerializeField] private Vector2 from = new Vector2(-1400f, 400f);
    [SerializeField] private Vector2 to   = new Vector2( 1400f, -400f);

    [Header("Timing")]
    [SerializeField] private float inTime  = 0.06f;
    [SerializeField] private float outTime = 0.22f;

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
            slash.anchoredPosition = Vector2.LerpUnclamped(from, to, k);

            c.a = t < inTime
                ? Mathf.Lerp(0f, peakAlpha, t / inTime)
                : Mathf.Lerp(peakAlpha, 0f, (t - inTime) / outTime);
            slashImage.color = c;
            yield return null;
        }

        c.a = 0f; slashImage.color = c;
    }
}
