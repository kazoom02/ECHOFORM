using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — IdleLoopSfx
// A looping ambient sound tied to the GameObject being active:
// it starts on OnEnable and stops on OnDisable. Perfect for
// VestigeIdle / SlimeIdle — a continuous hum that plays while the
// character is alive on screen, WITHOUT re-triggering every time
// the idle animation loops (which is why Animation Events are a
// bad fit for looping ambience).
//
// Route it to the SFX mixer group so the SFX slider controls it.
// Optional fade in/out so it doesn't pop on spawn/death.
// =====================================================

[RequireComponent(typeof(AudioSource))]
public class IdleLoopSfx : MonoBehaviour
{
    [Header("Loop")]
    [SerializeField] private AudioClip loopClip;
    [Tooltip("Route to the SFX group of your AudioMixer so the SFX slider controls it.")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;

    [Header("Fades")]
    [Tooltip("Seconds to fade in when it becomes active. 0 = start instantly.")]
    [SerializeField] private float fadeInSeconds = 0.25f;
    [Tooltip("Random start offset so multiple copies (e.g. 3 slimes) don't phase-sync.")]
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
