using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — SfxPlayer
// Reproduz efeitos sonoros pontuais através do misturador de áudio e permite
// que continuem a tocar mesmo após a destruição do objeto emissor.
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

        public void Play(AudioClip clip)
    {
        if (clip == null || src == null) return;
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.PlayOneShot(clip, volume);
    }

        public void PlayClip(AudioClip clip) => Play(clip);

        public void Play()
    {
        if (defaultClip != null) Play(defaultClip);
    }

        public void PlayDetached(AudioClip clip) => PlayAt(clip, sfxGroup, transform.position, volume);

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
