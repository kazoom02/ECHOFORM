using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — EnemyMeleeAnimator
// Enemy-side mirror of VestigeCombatAnimator: on its turn the
// creature walks across to the player, deals its damage on the
// hit frame, then walks back to its slot. CombatManager yields
// this during the attack phase; any enemy WITHOUT this component
// just deals damage in place (unchanged behaviour).
//
// Uses Animator.Play(stateName), so each tier's controller only
// needs "Idle" and "Walk" states (an "Attack" state is optional).
// Because the walk moves to the player's Y, pair it with a
// YDepthSorter on the prefab so the walker sorts correctly.
// =====================================================

public class EnemyMeleeAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animator state names")]
    [SerializeField] private string idleState   = "Idle";
    [SerializeField] private string walkState    = "Walk";
    [Tooltip("Optional. Leave blank if the tier has no separate attack clip — it will just lunge and hit.")]
    [SerializeField] private string attackState  = "";

    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 4f;
    [SerializeField] private float stopDistance = 1.2f;   // how far short of the player to stop
    [SerializeField] private bool  returnToStart = true;

    [Header("Timing")]
    [Tooltip("Seconds after arriving before damage lands.")]
    [SerializeField] private float hitTime      = 0.20f;
    [Tooltip("Total attack beat; walk-back starts after this.")]
    [SerializeField] private float attackLength = 0.40f;

    [Header("Facing")]
    [SerializeField] private bool faceByFlipX   = true;
    [Tooltip("On if the sprite art points RIGHT by default. Slimes usually face left toward the player — turn this off then.")]
    [SerializeField] private bool artFacesRight = false;

    [Header("Hit VFX")]
    [Tooltip("Electricity particle prefab, spawned on the hit frame. Leave null for no VFX.")]
    [SerializeField] private ParticleSystem hitEffectPrefab;
    [Tooltip("On = spawn the zap on the player. Off = spawn it at this creature (e.g. a charge-up).")]
    [SerializeField] private bool spawnAtTarget = true;
    [Tooltip("Extra world offset applied to the spawn point (e.g. raise it to chest height).")]
    [SerializeField] private Vector3 hitEffectOffset = Vector3.zero;
    [Tooltip("Sorting layer for the spawned VFX. Leave blank to keep the prefab's layer.")]
    [SerializeField] private string hitEffectSortingLayer = "";
    [Tooltip("Order in layer for the spawned VFX. High = drawn in front of the characters.")]
    [SerializeField] private int hitEffectSortingOrder = 5000;

    void Awake()
    {
        if (animator == null)       animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>Walk to the target, fire onHit on the hit frame, walk back. Yield this from a coroutine.</summary>
    public IEnumerator PlayAttack(Transform target, System.Action onHit)
    {
        if (target == null) { onHit?.Invoke(); yield break; }

        Vector3 start = transform.position;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        if (dir == 0f) dir = 1f;
        Face(dir);

        // Walk to the player's X (short of it) AND depth (Y), so the YDepthSorter
        // places the walker correctly among Vestige and the other slimes.
        Vector3 dest = new Vector3(target.position.x - dir * stopDistance, target.position.y, transform.position.z);

        Play(walkState);
        while ((transform.position - dest).sqrMagnitude > 0.0025f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (!string.IsNullOrEmpty(attackState)) Play(attackState);
        if (hitTime > 0f) yield return new WaitForSeconds(hitTime);
        SpawnHitEffect(target);                            // electric zap on the hit frame
        onHit?.Invoke();                                   // <-- damage lands here
        float rest = attackLength - hitTime;
        if (rest > 0f) yield return new WaitForSeconds(rest);

        if (returnToStart)
        {
            float backDir = Mathf.Sign(start.x - transform.position.x);
            if (backDir != 0f) Face(backDir);
            Play(walkState);
            while ((transform.position - start).sqrMagnitude > 0.0025f)
            {
                transform.position = Vector3.MoveTowards(transform.position, start, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = start;
        }

        Face(Mathf.Sign(target.position.x - transform.position.x));   // face the player again
        Play(idleState);
    }

    void SpawnHitEffect(Transform target)
    {
        if (hitEffectPrefab == null) return;

        Vector3 at = (spawnAtTarget && target != null ? target.position : transform.position)
                     + hitEffectOffset;

        ParticleSystem fx = Instantiate(hitEffectPrefab, at, Quaternion.identity);

        // Force it in front of the character sprites (the Renderer module's
        // Sorting Layer / Order in Layer, set here so you don't have to dig
        // into the particle Inspector).
        var r = fx.GetComponent<ParticleSystemRenderer>();
        if (r != null)
        {
            if (!string.IsNullOrEmpty(hitEffectSortingLayer))
                r.sortingLayerName = hitEffectSortingLayer;
            r.sortingOrder = hitEffectSortingOrder;
        }

        fx.Play();

        // self-clean once it has finished, even if Stop Action isn't set to Destroy
        float life = fx.main.duration + fx.main.startLifetime.constantMax;
        Destroy(fx.gameObject, life);
    }

    void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state);
    }

    void Face(float dir)
    {
        if (dir == 0f || spriteRenderer == null) return;
        bool movingLeft = dir < 0f;
        if (faceByFlipX)
        {
            spriteRenderer.flipX = artFacesRight ? movingLeft : !movingLeft;
        }
        else
        {
            Vector3 s = transform.localScale;
            float sign = artFacesRight ? (movingLeft ? -1f : 1f) : (movingLeft ? 1f : -1f);
            s.x = Mathf.Abs(s.x) * sign;
            transform.localScale = s;
        }
    }
}
