using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — EnemyMeleeAnimator
// Executa a coreografia de ataque corpo a corpo dos inimigos: aproximação,
// golpe no jogador e regresso à posição inicial.
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
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private bool  returnToStart = true;
    [Tooltip("If on, the enemy walks to the target's Y for depth sorting. Turn off for enemies that should stay on their lane.")]
    [SerializeField] private bool matchTargetY = true;
    [Tooltip("Expands Stop Distance using both sprites' visible widths. Useful for enemies that grow between tiers.")]
    [SerializeField] private bool useRendererBoundsForStopDistance;
    [Tooltip("Small visible gap between the enemy and target when renderer-bounds spacing is enabled.")]
    [SerializeField] private float contactPadding = 0.15f;

    [Header("Timing")]
    [Tooltip("Seconds after arriving before damage lands.")]
    [SerializeField] private float hitTime      = 0.20f;
    [Tooltip("Total attack beat; walk-back starts after this.")]
    [SerializeField] private float attackLength = 0.40f;

    [Header("Facing")]
    [SerializeField] private bool faceByFlipX   = true;
    [Tooltip("On if the sprite art points RIGHT by default. Slimes usually face left toward the player — turn this off then.")]
    [SerializeField] private bool artFacesRight = false;
    [Tooltip("Invert facing only while Attack plays. Use when the attack sheet faces opposite to the idle/walk sheets.")]
    [SerializeField] private bool invertAttackFacing;

    [Header("SFX")]
    [Tooltip("SfxPlayer that plays this creature's swing (auto-found on this object if left empty).")]
    [SerializeField] private SfxPlayer sfx;
    [Tooltip("Attack swing sound. Leave empty for silent attackers (e.g. slimes have no swing SFX).")]
    [SerializeField] private AudioClip attackSfx;

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
        if (sfx == null)            sfx = GetComponent<SfxPlayer>();
    }

        public IEnumerator PlayAttack(Transform target, System.Action onHit)
    {
        if (target == null) { onHit?.Invoke(); yield break; }

        Vector3 start = transform.position;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        if (dir == 0f) dir = 1f;
        Face(dir);

        float destY = matchTargetY ? target.position.y : start.y;
        float effectiveStopDistance = GetStopDistance(target);
        Vector3 dest = new Vector3(target.position.x - dir * effectiveStopDistance, destY, transform.position.z);

        Play(walkState);
        while ((transform.position - dest).sqrMagnitude > 0.0025f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }

        bool flipBeforeAttack = spriteRenderer != null && spriteRenderer.flipX;
        Vector3 scaleBeforeAttack = transform.localScale;
        if (invertAttackFacing)
        {
            if (faceByFlipX && spriteRenderer != null)
                spriteRenderer.flipX = !spriteRenderer.flipX;
            else
            {
                Vector3 invertedScale = transform.localScale;
                invertedScale.x *= -1f;
                transform.localScale = invertedScale;
            }
        }

        if (!string.IsNullOrEmpty(attackState)) Play(attackState);
        if (sfx != null && attackSfx != null) sfx.Play(attackSfx);
        if (hitTime > 0f) yield return new WaitForSeconds(hitTime);
        SpawnHitEffect(target);
        onHit?.Invoke();
        float rest = attackLength - hitTime;
        if (rest > 0f) yield return new WaitForSeconds(rest);

        if (invertAttackFacing)
        {
            if (faceByFlipX && spriteRenderer != null)
                spriteRenderer.flipX = flipBeforeAttack;
            else
                transform.localScale = scaleBeforeAttack;
        }

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

        Face(Mathf.Sign(target.position.x - transform.position.x));
        Play(idleState);
    }

    void SpawnHitEffect(Transform target)
    {
        if (hitEffectPrefab == null) return;

        Vector3 at = (spawnAtTarget && target != null ? target.position : transform.position)
                     + hitEffectOffset;

        ParticleSystem fx = Instantiate(hitEffectPrefab, at, Quaternion.identity);

        var r = fx.GetComponent<ParticleSystemRenderer>();
        if (r != null)
        {
            if (!string.IsNullOrEmpty(hitEffectSortingLayer))
                r.sortingLayerName = hitEffectSortingLayer;
            r.sortingOrder = hitEffectSortingOrder;
        }

        fx.Play();

        float life = fx.main.duration + fx.main.startLifetime.constantMax;
        Destroy(fx.gameObject, life);
    }

    float GetStopDistance(Transform target)
    {
        float result = stopDistance;
        if (!useRendererBoundsForStopDistance || spriteRenderer == null || target == null)
            return result;

        SpriteRenderer targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer == null) return result;

        float visibleSpacing = spriteRenderer.bounds.extents.x
                             + targetRenderer.bounds.extents.x
                             + Mathf.Max(0f, contactPadding);
        return Mathf.Max(result, visibleSpacing);
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
