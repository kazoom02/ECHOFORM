using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — WalkOffOnVictory
// Faz a personagem caminhar automaticamente até à saída após uma vitória,
// permitindo que atravesse o gatilho responsável pela transição de área.
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
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Animator state names — must exist in the character's controller.")]
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string idleState = "Idle";
    [Tooltip("Flip via SpriteRenderer.flipX (assumes art faces right). Off = flip localScale.x.")]
    [SerializeField] private bool faceByFlipX = true;

    private Rigidbody2D body;
    private VestigeCombatAnimator combatAnimator;
    private bool started;

    public void Configure(CombatManager combatManager, Transform characterToMove, Transform destination)
    {
        if (isActiveAndEnabled && combat != null)
            combat.OnStateChanged -= OnCombatState;

        combat = combatManager;
        character = characterToMove != null ? characterToMove : transform;
        exitTarget = destination;
        CacheCharacterComponents();

        if (isActiveAndEnabled && combat != null)
        {
            combat.OnStateChanged -= OnCombatState;
            combat.OnStateChanged += OnCombatState;
            StartCoroutine(CheckExistingVictoryNextFrame());
        }
    }

    void Awake()
    {
        if (character == null) character = transform;
        CacheCharacterComponents();
    }

    private void CacheCharacterComponents()
    {
        body = character.GetComponent<Rigidbody2D>();
        combatAnimator = character.GetComponent<VestigeCombatAnimator>();
        if (animator == null)       animator = character.GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = character.GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (combat != null)
        {
            combat.OnStateChanged -= OnCombatState;
            combat.OnStateChanged += OnCombatState;

            StartCoroutine(CheckExistingVictoryNextFrame());
        }
    }

    private IEnumerator CheckExistingVictoryNextFrame()
    {
        yield return null;
        if (combat != null && combat.State == CombatState.Win) TryStart();
    }

    void OnDisable()
    {
        if (combat != null) combat.OnStateChanged -= OnCombatState;

        if (started) StopAndIdle();
    }

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
        Debug.Log($"[WalkOffOnVictory] {character.name} is walking to {exitTarget?.name ?? "the exit"}.", this);
        StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (combatAnimator != null && combatAnimator.IsAnimating)
            yield return null;

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
            EnsureWalkState();
            Vector2 next = Vector2.MoveTowards(character.position, exitTarget.position,
                                               walkSpeed * Time.fixedDeltaTime);
            if (body != null) body.MovePosition(next);
            else character.position = next;
            yield return step;
        }

        Play(idleState);
    }

    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state);
    }

    private void EnsureWalkState()
    {
        if (animator == null || string.IsNullOrEmpty(walkState)) return;
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(walkState))
            animator.Play(walkState, 0, 0f);
    }

    private void Face(float dir)
    {
        if (dir == 0f) return;
        if (faceByFlipX && spriteRenderer != null)
        {
            spriteRenderer.flipX = dir < 0f;
        }
        else
        {
            Vector3 s = character.localScale;
            s.x = Mathf.Abs(s.x) * (dir < 0f ? -1f : 1f);
            character.localScale = s;
        }
    }
}
