using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — OverloadReadout
// Apresenta o nível de corrupção da memória, altera o aviso visual conforme
// o risco e indica o dano provocado pela sobrecarga.
// =====================================================

public class OverloadReadout : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image[] pips;
    [SerializeField] private Sprite pipOn;
    [SerializeField] private Sprite pipOff;
    [SerializeField] private int overloadAt = 5;

    static readonly Color Stable = new Color(0.30f, 0.95f, 0.75f);
    static readonly Color Warn   = new Color(1.00f, 0.70f, 0.15f);
    static readonly Color Danger = new Color(1.00f, 0.25f, 0.30f);

    public void Set(int corrupted)
    {
        corrupted = Mathf.Clamp(corrupted, 0, overloadAt);
        int copyNo = Mathf.RoundToInt(corrupted * (10f / overloadAt));

        if (label)
        {
            if (corrupted <= 0)
            {
                label.text = "MEMORY STABLE";
                label.color = Stable;
            }
            else if (corrupted >= overloadAt)
            {
                label.text = "COPY #10 - OVERWRITTEN";
                label.color = Danger;
            }
            else if (corrupted >= 3)
            {
                label.text = $"OVERWRITE #{copyNo}/10 - {corrupted - 2} DMG/TURN";
                label.color = Danger;
            }
            else
            {
                label.text = $"COPY #{copyNo}/10 PENDING";
                label.color = Warn;
            }
        }

        if (pips != null)
            for (int i = 0; i < pips.Length; i++)
                if (pips[i]) pips[i].sprite = i < corrupted ? pipOn : pipOff;
    }
}
