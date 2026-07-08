using UnityEngine;

// =====================================================
// ECHOFORM — EnemySelectable
// Marks an enemy as clickable during target selection and
// toggles its hover border. Needs a Collider2D so the
// targeting raycast can hit it. Put a border child (the
// TargetReticle sprite), hidden by default, in `highlight`.
// =====================================================

[RequireComponent(typeof(Collider2D))]
public class EnemySelectable : MonoBehaviour
{
    [SerializeField] private GameObject highlight;   // border child (TargetReticle), off by default

    public Enemy Enemy { get; private set; }

    void Awake()
    {
        Enemy = GetComponent<Enemy>();
        SetHighlight(false);
    }

    public void SetHighlight(bool on)
    {
        if (highlight) highlight.SetActive(on);
    }
}
