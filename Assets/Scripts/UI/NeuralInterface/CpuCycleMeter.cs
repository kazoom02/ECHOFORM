using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — CpuCycleMeter
// The "CPU Cycles ● ● ●" energy readout. Assign the pip
// Images left-to-right and the two pip sprites. Call
// Set(current, max) whenever energy changes.
// =====================================================

public class CpuCycleMeter : MonoBehaviour
{
    [SerializeField] private Image[] pips;      // ordered left -> right
    [SerializeField] private Sprite onSprite;   // CpuPip_On
    [SerializeField] private Sprite offSprite;  // CpuPip_Off

    public void Set(int current, int max)
    {
        for (int i = 0; i < pips.Length; i++)
        {
            if (!pips[i]) continue;
            pips[i].gameObject.SetActive(i < max);          // hide pips beyond max
            pips[i].sprite = i < current ? onSprite : offSprite;
        }
    }
}
