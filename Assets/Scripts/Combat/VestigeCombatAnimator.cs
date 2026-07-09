using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — VestigeCombatAnimator
// Melee choreography for an attack chip:
//   walk to the target  ->  play the attack animation  ->
//   deal damage on the hit frame  ->  walk back to idle.
// Uses Animator.Play(stateName), so you only need the three
// states to exist in Vestige's Animator Controller — no
// parameters or transitions to wire.
// =====================================================

public class VestigeCombatAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;   // used for facing (optional)

    [Header("Animator state names (must exist in the controller)")]
    [SerializeField] private string idleState   = "Idle";
    [SerializeField] private string walkState   = "Walk";
    [SerializeField] private string attackState = "Attack";

    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 6f;
    [SerializeField] private float stopDistance = 1.5f;   // how far short of the target to stop
    [SerializeField] private bool  returnToStart = true;

    [Header("Attack timing")]
    [Tooltip("Seconds into the attack animation when the hit lands (damage applied).")]
    [SerializeField] private float hitTime     = 0.30f;
    [Tooltip("Total length of the attack animation.")]
    [SerializeField] private float attackLength = 0.60f;
    [Tooltip("Flip via SpriteRenderer.flipX (assumes art faces right). Off = flip localScale.x.")]
    [SerializeField] private bool  faceByFlipX = true;

    void Awake()
    {
        if (animator == null)       animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>Walk to target, attack, fire onHit on the hit frame, then return. Yield this from a coroutine.</summary>
    public IEnumerator PlayAttack(Transform target, System.Action onHit)
    {
        if (target == null) { onHit?.Invoke(); yield break; }

        Vector3 start = transform.position;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        if (dir == 0f) dir = 1f;
        Face(dir);

        // Walk to the target's DEPTH (its Y), not just its X. Combined with a
        // YDepthSorter on Vestige, arriving at the slime's Y makes him weave
        // into the row — in front of nearer slimes, behind farther ones —
        // instead of sliding through them on a fixed layer.
        Vector3 dest = new Vector3(target.position.x - dir * stopDistance, target.position.y, transform.position.z);

        // walk in
        Play(walkState);
        while (Mathf.Abs(transform.position.x - dest.x) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // attack
        Play(attackState);
        if (hitTime > 0f) yield return new WaitForSeconds(hitTime);
        onHit?.Invoke();                                   // <-- damage lands here
        float rest = attackLength - hitTime;
        if (rest > 0f) yield return new WaitForSeconds(rest);

        // walk back
        if (returnToStart)
        {
            float backDir = Mathf.Sign(start.x - transform.position.x);
            if (backDir != 0f) Face(backDir);
            Play(walkState);
            while (Mathf.Abs(transform.position.x - start.x) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, start, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = start;
            Face(dir);   // face back toward the enemies
        }

        Play(idleState);
    }

    void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state);
    }

    void Face(float dir)
    {
        if (dir == 0f) return;
        if (faceByFlipX && spriteRenderer != null)
        {
            spriteRenderer.flipX = dir < 0f;             // assumes the art faces right
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir < 0f ? -1f : 1f);
            transform.localScale = s;
        }
    }
}
