using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================
// ECHOFORM — OverloadReadout
// The on-screen fail-state fiction: how close Vestige is to
// being overwritten by the Loom. Maps corrupted-chip count
// (0..overloadAt) onto a "COPY #n/10" scale — 5 corrupted = №10,
// matching the loss log. Escalates color and warns of damage.
// Call Set(corruptedInHand) whenever the hand changes.
// =====================================================

public class OverloadReadout : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image[] pips;        // optional red warning pips, left -> right
    [SerializeField] private Sprite pipOn;        // CopyPip_On
    [SerializeField] private Sprite pipOff;       // CopyPip_Off
    [SerializeField] private int overloadAt = 5;  // corrupted slots that = death

    static readonly Color Stable = new Color(0.30f, 0.95f, 0.75f);
    static readonly Color Warn   = new Color(1.00f, 0.70f, 0.15f);
    static readonly Color Danger = new Color(1.00f, 0.25f, 0.30f);

    public void Set(int corrupted)
    {
        corrupted = Mathf.Clamp(corrupted, 0, overloadAt);
        int copyNo = Mathf.RoundToInt(corrupted * (10f / overloadAt));   // 0..5 -> 0,2,4,6,8,10

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
