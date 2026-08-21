using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — IdleLoopSfx
// Gere um som ambiente em repetição enquanto o objeto está ativo,
// com início gradual e possibilidade de começar num ponto aleatório.
// =====================================================

[RequireComponent(typeof(AudioSource))]
public class IdleLoopSfx : MonoBehaviour
{
    [Header("Loop")]
    [SerializeField] private AudioClip loopClip;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;

    [Header("Fades")]
    [Tooltip("Seconds to fade in when it becomes active. 0 = start instantly.")]
    [SerializeField] private float fadeInSeconds = 0.25f;
    [SerializeField] private bool randomStartOffset = true;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.clip = loopClip;
        if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
    }

    void OnEnable()
    {
        if (loopClip == null) return;
        src.clip = loopClip;
        src.loop = true;
        if (randomStartOffset) src.time = Random.Range(0f, loopClip.length);
        src.Play();

        if (fadeInSeconds > 0f) StartCoroutine(FadeIn());
        else src.volume = volume;
    }

    void OnDisable()
    {
        if (src != null) src.Stop();
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        src.volume = 0f;
        while (t < fadeInSeconds)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, volume, t / fadeInSeconds);
            yield return null;
        }
        src.volume = volume;
    }
}
