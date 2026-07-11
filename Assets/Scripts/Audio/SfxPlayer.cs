using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — SfxPlayer
// One-shot sound effects, routed to the SFX mixer group so the
// SFX volume slider (SFXVol) controls them. Put it on any object
// that needs to make noise (Vestige, a slime).
//
// Two ways to fire it:
//   1) From code:            sfx.Play(slashClip);
//   2) From an Animation Event: add an event on the clip frame,
//      choose PlayClip, and drag the AudioClip into the slot.
//
// Uses PlayOneShot so rapid/overlapping hits don't cut each other.
// PlayDetached / PlayAt play on a throwaway object so the clip
// finishes even if the emitter is destroyed (slime split, a VFX
// prefab that self-destructs like the charged slash).
// =====================================================

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    [Header("Routing")]
    [Tooltip("Route to the SFX group of your AudioMixer so the SFX slider controls it.")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Optional default clip")]
    [Tooltip("If set, a parameterless Play() / animation event with no arg plays this.")]
    [SerializeField] private AudioClip defaultClip;

    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    [Tooltip("Small random pitch spread so repeated hits don't sound identical. 0 = off.")]
    [Range(0f, 0.5f)] [SerializeField] private float pitchJitter = 0.05f;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
    }

    /// <summary>Play a specific clip. Callable from code or an Animation Event (Object param).</summary>
    public void Play(AudioClip clip)
    {
        if (clip == null || src == null) return;
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.PlayOneShot(clip, volume);
    }

    /// <summary>Animation-Event-friendly alias (some Unity versions prefer this name in the dropdown).</summary>
    public void PlayClip(AudioClip clip) => Play(clip);

    /// <summary>Parameterless — plays the default clip.</summary>
    public void Play()
    {
        if (defaultClip != null) Play(defaultClip);
    }

    /// <summary>Play on a throwaway object that outlives this emitter (uses this player's group/volume).</summary>
    public void PlayDetached(AudioClip clip) => PlayAt(clip, sfxGroup, transform.position, volume);

    /// <summary>
    /// Static: play a clip on a throwaway GameObject that self-destroys when the
    /// clip ends, routed to the given mixer group. Use when the caller has no
    /// SfxPlayer of its own or is about to be destroyed (e.g. a VFX prefab).
    /// </summary>
    public static void PlayAt(AudioClip clip, AudioMixerGroup group, Vector3 pos, float volume = 1f)
    {
        if (clip == null) return;
        var go = new GameObject("SFX_" + clip.name);
        go.transform.position = pos;
        var a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.volume = volume;
        if (group != null) a.outputAudioMixerGroup = group;
        a.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }
}
