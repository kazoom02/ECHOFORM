using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — VestigeCombatAnimator
// Executa a coreografia dos ataques do Vestige: aproximação ao alvo,
// animação do golpe, aplicação do dano e regresso à posição inicial.
// =====================================================

public class VestigeCombatAnimator : MonoBehaviour
{
    public bool IsAnimating { get; private set; }

    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animator state names (must exist in the controller)")]
    [SerializeField] private string idleState   = "Idle";
    [SerializeField] private string walkState   = "Walk";
    [SerializeField] private string attackState = "Attack";

    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 6f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private bool  returnToStart = true;

    [Header("Attack timing")]
    [Tooltip("Seconds into the attack animation when the hit lands (damage applied).")]
    [SerializeField] private float hitTime     = 0.30f;
    [Tooltip("Total length of the attack animation.")]
    [SerializeField] private float attackLength = 0.60f;
    [Tooltip("Flip via SpriteRenderer.flipX (assumes art faces right). Off = flip localScale.x.")]
    [SerializeField] private bool  faceByFlipX = true;

    [Header("SFX")]
    [Tooltip("SfxPlayer on this object (auto-found if left empty).")]
    [SerializeField] private SfxPlayer sfx;
    [Tooltip("Melee swing sound (e.g. SlashV2).")]
    [SerializeField] private AudioClip attackSfx;

    [Header("Death")]
    [Tooltip("Animator state played when Vestige dies. Already exists in the MainCharacter controller as \"Death\".")]
    [SerializeField] private string deathState = "Death";
    [Tooltip("Sound played on death (e.g. VestigeDeath).")]
    [SerializeField] private AudioClip deathSfx;

    void Awake()
    {
        if (animator == null)       animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (sfx == null)            sfx = GetComponent<SfxPlayer>();
    }

        public IEnumerator PlayAttack(Transform target, System.Action onHit)
    {
        if (target == null) { onHit?.Invoke(); yield break; }

        IsAnimating = true;

        Vector3 start = transform.position;

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        if (dir == 0f) dir = 1f;
        Face(dir);

        Vector3 dest = new Vector3(target.position.x - dir * stopDistance, target.position.y, transform.position.z);

        Play(walkState);
        while (Mathf.Abs(transform.position.x - dest.x) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }

        Play(attackState);
        if (sfx != null) sfx.Play(attackSfx);
        if (hitTime > 0f) yield return new WaitForSeconds(hitTime);
        onHit?.Invoke();
        float rest = attackLength - hitTime;
        if (rest > 0f) yield return new WaitForSeconds(rest);

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
            Face(dir);
        }

        Play(idleState);
        IsAnimating = false;
    }

    void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state);
    }

        public void PlayDeath()
    {
        IsAnimating = false;
        StopAllCoroutines();
        Play(deathState);
        if (sfx != null) sfx.PlayDetached(deathSfx);
    }

    private void OnDisable()
    {
        IsAnimating = false;
    }

    void Face(float dir)
    {
        if (dir == 0f) return;
        if (faceByFlipX && spriteRenderer != null)
        {
            spriteRenderer.flipX = dir < 0f;
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir < 0f ? -1f : 1f);
            transform.localScale = s;
        }
    }
}
