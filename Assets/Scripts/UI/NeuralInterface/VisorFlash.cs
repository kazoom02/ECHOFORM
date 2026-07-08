using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — VisorFlash
// Flashes Vestige's visor cyan when a memory installs.
// Put an Image over the visor (or a full-screen overlay
// Image) and assign it. Call Flash() to trigger.
// =====================================================

public class VisorFlash : MonoBehaviour
{
    [SerializeField] private Image visor;                                   // image tinted during the flash
    [SerializeField] private Color flashColor = new Color(0.04f, 1f, 1f);   // cyan
    [SerializeField] private float peakAlpha = 0.9f;
    [SerializeField] private float flashIn  = 0.05f;
    [SerializeField] private float flashOut = 0.28f;

    void Awake()
    {
        if (visor) { var c = flashColor; c.a = 0f; visor.color = c; }
    }

    public void Flash()
    {
        if (!visor) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Color c = flashColor;
        float t = 0f;
        while (t < flashIn)  { t += Time.deltaTime; c.a = Mathf.Lerp(0f, peakAlpha, t / flashIn);  visor.color = c; yield return null; }
        t = 0f;
        while (t < flashOut) { t += Time.deltaTime; c.a = Mathf.Lerp(peakAlpha, 0f, t / flashOut); visor.color = c; yield return null; }
        c.a = 0f; visor.color = c;
    }
}
