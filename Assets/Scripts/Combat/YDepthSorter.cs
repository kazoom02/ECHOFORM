using UnityEngine;

// =====================================================
// ECHOFORM — YDepthSorter
// Calcula a ordem de desenho de um SpriteRenderer a partir da posição
// vertical no mundo, garantindo a sobreposição correta das personagens.
// =====================================================

[RequireComponent(typeof(SpriteRenderer))]
public class YDepthSorter : MonoBehaviour
{
    [Tooltip("Units of Y per sorting step. MUST match the slimes' basis (100 in RepositionEnemies).")]
    [SerializeField] private float sortingMultiplier = 100f;
    [Tooltip("Added after the Y calc. Small positive = win ties (render in front) at equal depth.")]
    [SerializeField] private int orderBias = 20;
    [Tooltip("Which Y to read. Leave null to use this object's own transform.")]
    [SerializeField] private Transform source;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (source == null) source = transform;
    }

    void LateUpdate()
    {
        if (sr == null) return;
        sr.sortingOrder = Mathf.RoundToInt(-source.position.y * sortingMultiplier) + orderBias;
    }
}
