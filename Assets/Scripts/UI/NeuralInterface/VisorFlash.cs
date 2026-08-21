using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — VisorFlash
// Faz a viseira do Vestige emitir um breve clarão ciano quando uma memória
// é instalada.
// =====================================================

public class VisorFlash : MonoBehaviour
{
    [SerializeField] private Image visor;
    [SerializeField] private Color flashColor = new Color(0.04f, 1f, 1f);
    [SerializeField] private float peakAlpha = 0.9f;
    [SerializeField] private float flashIn  = 0.05f;
    [SerializeField] private float flashOut = 0.28f;

    public float Duration => flashIn + flashOut;

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

    public IEnumerator FlashAndWait()
    {
        Flash();
        yield return new WaitForSeconds(Duration);
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
