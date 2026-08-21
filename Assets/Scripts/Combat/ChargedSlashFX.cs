using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — ChargedSlashFX
// Controla o efeito visual do golpe carregado, ajusta-o à câmara,
// reproduz o som associado e destrói-o no fim da animação.
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

    [Header("SFX")]
    [Tooltip("Sound played when this slash spawns (e.g. HeavySlash).")]
    [SerializeField] private AudioClip slashSfx;
    [Tooltip("Route to the SFX group of your AudioMixer so the SFX slider controls it.")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private Animator animator;
    private SpriteRenderer sr;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (overrideSortingOrder && sr != null)
            sr.sortingOrder = sortingOrder;

        SfxPlayer.PlayAt(slashSfx, sfxGroup, transform.position, sfxVolume);
    }

    void Start()
    {
        if (fitToCamera) StartCoroutine(FitToCameraRoutine());
        StartCoroutine(DestroyWhenDone());
    }

    private IEnumerator FitToCameraRoutine()
    {
        yield return null;

        Camera cam = fitCamera != null ? fitCamera : Camera.main;
        if (cam == null || sr == null || sr.sprite == null) yield break;

        Vector2 sprite = sr.sprite.bounds.size;
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

        Vector3 c = cam.transform.position;
        transform.position = new Vector3(c.x, c.y, transform.position.z);
    }

    private IEnumerator DestroyWhenDone()
    {

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

    public IEnumerator WaitUntilFinished()
    {
        yield return null;
        while (this != null) yield return null;
    }

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
