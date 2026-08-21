using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — CpuCycleMeter
// Apresenta os ciclos de CPU disponíveis através de indicadores visuais
// e atualiza-os de acordo com a energia atual e máxima.
// =====================================================

public class CpuCycleMeter : MonoBehaviour
{
    [SerializeField] private Image[] pips;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    public void Set(int current, int max)
    {
        for (int i = 0; i < pips.Length; i++)
        {
            if (!pips[i]) continue;
            pips[i].gameObject.SetActive(i < max);
            pips[i].sprite = i < current ? onSprite : offSprite;
        }
    }
}
