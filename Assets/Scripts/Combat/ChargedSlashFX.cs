using System.Collections;
using UnityEngine;

// =====================================================
// ECHOFORM — ChargedSlashFX
// Fire-and-forget controller for the charged katana slash VFX.
// Sits on the slash prefab (Sprite Renderer + Animator running
// the non-looping ChargedSlash clip). It plays once, then
// destroys itself when the clip ends — no manual cleanup.
//
// Spawn it from combat code with the static Play() helper:
//   ChargedSlashFX.Play(slashPrefab, hitPos, flipX: facingLeft);
// With Fit To Camera on, it auto-scales to fill the screen.
// =====================================================

[RequireComponent(typeof(Animator))]
public class ChargedSlashFX : MonoBehaviour
{
    [Header("Cleanup")]
    [Tooltip("Extra seconds kept alive after the clip finishes.")]
    [SerializeField] private float extraLifetime = 0.05f;
    [Tooltip("Fallback lifetime used if no Animator clip is found.")]
    [SerializeField] private float fallbackLifetime = 0.7f;

    [Header("Rendering")]
    [Tooltip("If set, forces the sprite's sorting order so the slash draws on top.")]
    [SerializeField] private bool overrideSortingOrder = false;
    [SerializeField] private int sortingOrder = 100;

    [Header("Fullscreen fit")]
    [Tooltip("Scale + center the slash to fill the camera view when it spawns.")]
    [SerializeField] private bool fitToCamera = true;
    [Tooltip("Camera to fit. Leave empty to use Camera.main.")]
    [SerializeField] private Camera fitCamera;
    [Tooltip("1 = exact fill. >1 overscans so edges are never visible.")]
    [SerializeField] private float fitOverscan = 1.02f;

    private Animator animator;
    private SpriteRenderer sr;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (overrideSortingOrder && sr != null)
            sr.sortingOrder = sortingOrder;
    }

    void Start()
    {
        if (fitToCamera) StartCoroutine(FitToCameraRoutine());
        StartCoroutine(DestroyWhenDone());
    }

    // Scale the slash so it covers the whole camera view, then center it.
    // Runs one frame late so the Animator has assigned the first sprite
    // (all frames share the same size, so any frame gives correct bounds).
    private IEnumerator FitToCameraRoutine()
    {
        yield return null;

        Camera cam = fitCamera != null ? fitCamera : Camera.main;
        if (cam == null || sr == null || sr.sprite == null) yield break;

        Vector2 sprite = sr.sprite.bounds.size;          // world units at scale 1
        if (sprite.x <= 0f || sprite.y <= 0f) yield break;

        float worldH, worldW;
        if (cam.orthographic)
        {
            worldH = cam.orthographicSize * 2f;
            worldW = worldH * cam.aspect;
        }
        else
        {
            float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
            worldH = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            worldW = worldH * cam.aspect;
        }

        float s = Mathf.Max(worldW / sprite.x, worldH / sprite.y) * fitOverscan;
        float signX = Mathf.Sign(transform.localScale.x == 0f ? 1f : transform.localScale.x);
        transform.localScale = new Vector3(s * signX, s, 1f);

        Vector3 c = cam.transform.position;              // center on the view
        transform.position = new Vector3(c.x, c.y, transform.position.z);
    }

    private IEnumerator DestroyWhenDone()
    {
        // Wait one frame so the Animator reports a valid current state.
        yield return null;

        float life = fallbackLifetime;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            float speed = Mathf.Max(0.01f, animator.speed);
            if (state.length > 0f)
                life = state.length / speed;
        }

        Destroy(gameObject, life + extraLifetime);
    }

    // -----------------------------------------------------
    // Spawn a slash instance. Returns the spawned component so the
    // caller can tweak it further if needed.
    //   position : world-space anchor (ignored if Fit To Camera is on)
    //   flipX    : mirror horizontally
    //   scale    : uniform scale multiplier (ignored if Fit To Camera is on)
    //   parent   : optional parent transform (null = world root)
    // -----------------------------------------------------
    public static ChargedSlashFX Play(
        ChargedSlashFX prefab,
        Vector3 position,
        bool flipX = false,
        float scale = 1f,
        Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning("ChargedSlashFX.Play called with a null prefab.");
            return null;
        }

        ChargedSlashFX fx = Instantiate(prefab, position, Quaternion.identity, parent);

        Vector3 s = fx.transform.localScale;
        fx.transform.localScale = new Vector3(
            Mathf.Abs(s.x) * (flipX ? -1f : 1f) * scale,
            Mathf.Abs(s.y) * scale,
            s.z);

        return fx;
    }
}
