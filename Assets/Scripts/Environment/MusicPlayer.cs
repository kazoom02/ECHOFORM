using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — MusicPlayer
// Reproduz em repetição a música associada a uma cena ou área enquanto
// o respetivo objeto está ativo, com encaminhamento e entrada gradual.
// =====================================================

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [Header("Track")]
    [SerializeField] private AudioClip track;
    [Tooltip("Route to the Music group of your AudioMixer so the Music slider controls it.")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private bool loop = true;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [Tooltip("Seconds to fade the track in. 0 = start instantly.")]
    [SerializeField] private float fadeInSeconds = 0.5f;

    private AudioSource src;
    private bool playbackDeferred;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.clip = track;
        if (musicGroup != null) src.outputAudioMixerGroup = musicGroup;
    }

    void OnEnable()
    {
        if (playbackDeferred)
        {
            if (src != null) src.Stop();
            return;
        }

        PlayTrack();
    }

    private void PlayTrack()
    {
        if (track == null)
        {
            Debug.LogWarning("[MusicPlayer] No track assigned.", this);
            return;
        }

        StopAllCoroutines();
        src.clip = track;
        src.loop = loop;
        src.Play();

        if (fadeInSeconds > 0f) StartCoroutine(FadeIn());
        else src.volume = volume;
    }

    void OnDisable()
    {
        if (src != null) src.Stop();
    }

    public void SetTrack(AudioClip clip)
    {
        track = clip;
        if (isActiveAndEnabled && !playbackDeferred) PlayTrack();
    }

    public void DeferPlayback()
    {
        playbackDeferred = true;
        StopAllCoroutines();
        if (src == null) src = GetComponent<AudioSource>();
        src.Stop();
    }

    public void PlayDeferred()
    {
        if (!playbackDeferred) return;
        playbackDeferred = false;
        if (isActiveAndEnabled) PlayTrack();
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        src.volume = 0f;
        while (t < fadeInSeconds)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(0f, volume, t / fadeInSeconds);
            yield return null;
        }
        src.volume = volume;
    }
}
