using UnityEngine;

// =====================================================
// ECHOFORM — YDepthSorter
// Drives a SpriteRenderer's sortingOrder from world Y so
// things lower on screen draw IN FRONT — the same basis
// CombatManager.RepositionEnemies uses for slimes
// (sortingOrder = -y * 100). Put this on Vestige so that as
// he walks into the row he passes in front of nearer slimes
// and behind farther ones, instead of clipping through them.
//
// orderBias nudges ties: give Vestige a small positive bias
// so when he stands at a slime's exact depth to attack it,
// he renders just in front of it.
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

    // LateUpdate: run after movement so the order reflects the final position.
    void LateUpdate()
    {
        if (sr == null) return;
        sr.sortingOrder = Mathf.RoundToInt(-source.position.y * sortingMultiplier) + orderBias;
    }
}
