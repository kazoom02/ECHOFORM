using UnityEngine;

// =====================================================
// ECHOFORM — EnemySelectable
// Associa um inimigo ao respetivo colisor e controla o realce visual
// apresentado durante a seleção de alvos.
// =====================================================

[RequireComponent(typeof(Collider2D))]
public class EnemySelectable : MonoBehaviour
{
    [SerializeField] private GameObject highlight;

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
