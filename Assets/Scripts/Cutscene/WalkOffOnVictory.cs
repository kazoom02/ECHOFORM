using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — WalkOffOnVictory
// When its CombatManager reaches Win (all monsters dead), the character
// auto-walks toward an off-screen target. It physically passes through
// the ExitTrigger on the way, which fires the AreaTransition.
//
// Animation matches VestigeCombatAnimator: it drives the Animator with
// Animator.Play(stateName) using the "Walk" / "Idle" states — no bool
// parameters or transitions to wire, the states just need to exist.
//
// Put one per area (e.g. under the Area GameObject so it only runs while
// that area is active). Assign that area's CombatManager and an
// exitTarget placed past the ExitTrigger, off-camera.
// =====================================================

public class WalkOffOnVictory : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("The encounter to watch. When it reaches Win, the walk starts.")]
    [SerializeField] private CombatManager combat;
    [Tooltip("Beat to wait after victory before the character starts walking.")]
    [SerializeField] private float startDelay = 0.6f;

    [Header("Who walks")]
    [Tooltip("The character to move. Defaults to this GameObject if left empty.")]
    [SerializeField] private Transform character;
    [Tooltip("Walk destination — place it past the ExitTrigger and off-camera.")]
    [SerializeField] private Transform exitTarget;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float arriveDistance = 0.05f;

    [Header("Animation (Animator.Play — same as VestigeCombatAnimator)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;   // for facing (optional)
    [Tooltip("Animator state names — must exist in the character's controller.")]
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string idleState = "Idle";
    [Tooltip("Flip via SpriteRenderer.flipX (assumes art faces right). Off = flip localScale.x.")]
    [SerializeField] private bool faceByFlipX = true;

    private Rigidbody2D body;
    private bool started;

    void Awake()
    {
        if (character == null) character = transform;
        body = character.GetComponent<Rigidbody2D>();
        if (animator == null)       animator = character.GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = character.GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (combat != null)
        {
            combat.OnStateChanged += OnCombatState;
            // In case combat was already won before this object activated.
            if (combat.State == CombatState.Win) TryStart();
        }
    }

    void OnDisable()
    {
        if (combat != null) combat.OnStateChanged -= OnCombatState;

        // If this area is deactivated mid-walk (the swap), don't leave the
        // shared character stuck in the Walk state.
        if (started) StopAndIdle();
    }

    // Stop walking immediately and drop to Idle. Hook this to the
    // AreaTransition's "On Swap" event so the character is idle the moment
    // the next area appears.
    public void StopAndIdle()
    {
        StopAllCoroutines();
        if (body != null) body.linearVelocity = Vector2.zero;
        Play(idleState);
    }

    private void OnCombatState(CombatState s)
    {
        if (s == CombatState.Win) TryStart();
    }

    private void TryStart()
    {
        if (started) return;
        started = true;
        StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        if (exitTarget == null)
        {
            Debug.LogWarning("[WalkOffOnVictory] No exitTarget assigned — nothing to walk to.", this);
            yield break;
        }

        float dir = Mathf.Sign(exitTarget.position.x - character.position.x);
        if (dir == 0f) dir = 1f;
        Face(dir);
        Play(walkState);

        var step = new WaitForFixedUpdate();
        while (Vector2.Distance(character.position, exitTarget.position) > arriveDistance)
        {
            Vector2 next = Vector2.MoveTowards(character.position, exitTarget.position,
                                               walkSpeed * Time.fixedDeltaTime);
            if (body != null) body.MovePosition(next);   // MovePosition so 2D triggers fire
            else character.position = next;
            yield return step;
        }

        Play(idleState);
    }

    // Same approach as VestigeCombatAnimator: play a state directly by name.
    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state);
    }

    private void Face(float dir)
    {
        if (dir == 0f) return;
        if (faceByFlipX && spriteRenderer != null)
        {
            spriteRenderer.flipX = dir < 0f;             // assumes the art faces right
        }
        else
        {
            Vector3 s = character.localScale;
            s.x = Mathf.Abs(s.x) * (dir < 0f ? -1f : 1f);
            character.localScale = s;
        }
    }
}
