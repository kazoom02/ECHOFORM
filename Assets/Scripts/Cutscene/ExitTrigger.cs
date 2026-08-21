using UnityEngine;

// =====================================================
// ECHOFORM — ExitTrigger
// Ativa uma transição de área quando o jogador entra na saída, podendo
// exigir que o combate da área tenha sido concluído.
// =====================================================

[RequireComponent(typeof(Collider2D))]
public class ExitTrigger : MonoBehaviour
{
    [Header("Gate")]
    [Tooltip("The encounter that must be won before this exit works. Leave empty to fire with no combat gate.")]
    [SerializeField] private CombatManager combat;
    [Tooltip("If on, the exit only arms once combat reaches Win.")]
    [SerializeField] private bool requireCombatWon = true;
    [Tooltip("Optional area owner. When assigned, this exit can only fire while that area is active.")]
    [SerializeField] private GameObject activeAreaGate;

    [Header("Action")]
    [SerializeField] private AreaTransition transition;
    [Tooltip("Only colliders with this tag trigger the exit. Empty = anything.")]
    [SerializeField] private string playerTag = "Player";

    private bool fired;

    public void Configure(CombatManager combatManager, AreaTransition areaTransition, string requiredPlayerTag = "Player", GameObject areaGate = null)
    {
        combat = combatManager;
        transition = areaTransition;
        playerTag = requiredPlayerTag;
        activeAreaGate = areaGate;
        requireCombatWon = true;
        fired = false;

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null) trigger.isTrigger = true;
    }

    public void SetActiveAreaGate(GameObject areaGate)
    {
        activeAreaGate = areaGate;
    }

    void Reset()
    {

        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private bool Armed =>
        (activeAreaGate == null || activeAreaGate.activeInHierarchy) &&
        (!requireCombatWon || (combat != null && combat.State == CombatState.Win));

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fired || !Armed) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        fired = true;
        if (transition != null) transition.Play();
        else Debug.LogWarning("[ExitTrigger] No AreaTransition assigned.", this);
    }
}
