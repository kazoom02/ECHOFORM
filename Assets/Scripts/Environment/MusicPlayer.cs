using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — MusicPlayer
// Plays one looping music track while its GameObject is active, and
// stops when the object is deactivated. That single rule covers both:
//   - Separate scenes  (Main Menu, Credits): plays on scene load.
//   - In-scene areas    (Area1/2/3 toggled by AreaTransition): the
//     activated area's track starts, the deactivated area's stops.
//
// Route it to the Music mixer group so your Music volume slider controls it.
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

    // Swap to a different track at runtime (optional helper).
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
