using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// =====================================================
// ECHOFORM — AreaTransition
// Reproduz um vídeo de transição dentro da cena e, enquanto o ecrã está
// coberto, troca a área ativa e reposiciona o jogador e a câmara.
// =====================================================

public class AreaTransition : MonoBehaviour
{
    private static AreaTransition activeTransition;
    public static bool IsPlaying => activeTransition != null && activeTransition.playing;

    [Header("Video overlay")]
    [Tooltip("VideoPlayer with the transition clip. isLooping is forced off.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("CanvasGroup on the fullscreen RawImage that shows the video's RenderTexture.")]
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private float fadeDuration = 0.4f;

    [Tooltip("Canvas holding the overlay. Forced to Screen Space - Overlay + a high sort order so the video draws ON TOP of the scene. Auto-found from the overlay if left empty.")]
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private bool forceOverlayOnTop = true;
    [SerializeField] private int overlaySortOrder = 100;
    [Tooltip("The RawImage that shows the video. Auto-found under the overlay if empty. Wired to the VideoPlayer's RenderTexture at runtime.")]
    [SerializeField] private RawImage videoImage;

    [Header("Area swap (done while the screen is covered)")]
    [Tooltip("Deactivated at the swap point, e.g. Area1.")]
    [SerializeField] private GameObject areaToHide;
    [Tooltip("Activated at the swap point, e.g. Area2.")]
    [SerializeField] private GameObject areaToShow;
    [Tooltip("Optional: something to reposition into Area2 (player or camera).")]
    [SerializeField] private Transform objectToTeleport;
    [Tooltip("Optional: where objectToTeleport should land in Area2.")]
    [SerializeField] private Transform teleportTarget;
    [Tooltip("Use a world-space destination without needing a separate teleport target GameObject.")]
    [SerializeField] private bool useDirectTeleportPosition;
    [SerializeField] private Vector3 directTeleportPosition;
    [Tooltip("Keep the incoming area's MusicPlayer silent until the transition video and reveal fade finish.")]
    [SerializeField] private bool delayIncomingMusicUntilFinished;

    [Header("Events")]
    [Tooltip("Fired when the transition begins (e.g. disable player input).")]
    public UnityEvent onTransitionStarted;
    [Tooltip("Fired at the swap point, while the video hides the cut.")]
    public UnityEvent onSwap;
    [Tooltip("Fired when the overlay has faded out and Area2 is visible (e.g. re-enable input).")]
    public UnityEvent onTransitionFinished;

    private bool playing;
    private bool skipSwapThisRun;
    private string sceneToLoadAfterPlayback;
    private MusicPlayer deferredIncomingMusic;

    void Awake()
    {

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }
        if (overlay != null)
        {
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
        }

        if (forceOverlayOnTop && overlay != null)
        {
            Canvas oc = overlay.GetComponent<Canvas>();
            if (oc == null) oc = overlay.gameObject.AddComponent<Canvas>();
            oc.overrideSorting = true;
            oc.sortingOrder = overlaySortOrder;
        }

        EnsureVideoWiring();
    }

    private void EnsureVideoWiring()
    {
        if (videoPlayer == null) return;

        if (videoImage == null && overlay != null)
            videoImage = overlay.GetComponentInChildren<RawImage>(true);

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        if (videoPlayer.targetTexture == null)
        {
            var rt = new RenderTexture(1920, 1080, 0) { name = "AreaTransitionRT (runtime)" };
            videoPlayer.targetTexture = rt;
        }

        if (videoImage != null)
        {
            videoImage.texture = videoPlayer.targetTexture;
            videoImage.color = Color.white;
            RectTransform rt = videoImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("[AreaTransition] No RawImage found under the overlay — assign 'Video Image'.", this);
        }
    }

    public void Play()
    {
        Begin(null, true);
    }

    public void PlayClipWithoutSwap(VideoClip clip)
    {
        Begin(clip, false);
    }

        public void PlayClipWithoutSwap(VideoClip clip, string nextScene)
    {
        sceneToLoadAfterPlayback = nextScene;
        Begin(clip, false);
    }

    private void Begin(VideoClip overrideClip, bool swapAreas)
    {
        if (playing || (activeTransition != null && activeTransition != this)) return;
        activeTransition = this;
        playing = true;
        CardTooltip.Hide();
        NeuralInterfaceHUD neuralHud = FindAnyObjectByType<NeuralInterfaceHUD>();
        if (neuralHud != null) neuralHud.ClearControllerSelection();
        skipSwapThisRun = !swapAreas;
        if (videoPlayer != null && overrideClip != null)
            videoPlayer.clip = overrideClip;
        gameObject.SetActive(true);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        onTransitionStarted?.Invoke();

        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.blocksRaycasts = true;
            yield return Fade(0f, 1f);
        }

        if (videoPlayer != null)
        {
            bool done = false;
            void OnEnd(VideoPlayer vp) => done = true;

            if (videoPlayer.clip == null && videoPlayer.source == VideoSource.VideoClip)
                Debug.LogWarning("[AreaTransition] VideoPlayer has no clip assigned — nothing will show.", this);

            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnEnd;
            videoPlayer.Play();

            yield return null;
            DoSwap();

            while (!done) yield return null;
            videoPlayer.loopPointReached -= OnEnd;
        }
        else
        {
            DoSwap();
        }

        if (overlay != null)
        {
            yield return Fade(1f, 0f);
            overlay.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
        }

        if (deferredIncomingMusic != null)
        {
            deferredIncomingMusic.PlayDeferred();
            deferredIncomingMusic = null;
        }

        onTransitionFinished?.Invoke();
        string nextScene = sceneToLoadAfterPlayback;
        sceneToLoadAfterPlayback = null;
        skipSwapThisRun = false;
        playing = false;
        if (activeTransition == this) activeTransition = null;

        if (!string.IsNullOrWhiteSpace(nextScene))
            SceneManager.LoadScene(nextScene);
    }

    private void OnDisable()
    {
        if (activeTransition == this) activeTransition = null;
    }

    private void DoSwap()
    {
        if (skipSwapThisRun) return;

        if (areaToHide != null) areaToHide.SetActive(false);

        deferredIncomingMusic = null;
        bool isArea3 = areaToShow != null &&
                       string.Equals(areaToShow.name.Trim(), "Area3", System.StringComparison.OrdinalIgnoreCase);
        if (areaToShow != null && (delayIncomingMusicUntilFinished || isArea3))
        {
            deferredIncomingMusic = areaToShow.GetComponent<MusicPlayer>();
            if (deferredIncomingMusic != null) deferredIncomingMusic.DeferPlayback();
        }

        if (areaToShow != null) areaToShow.SetActive(true);
        if (objectToTeleport != null)
        {
            if (teleportTarget != null)
                objectToTeleport.position = teleportTarget.position;
            else if (useDirectTeleportPosition)
                objectToTeleport.position = directTeleportPosition;
        }

        if (areaToShow != null)
        {
            AreaEncounter nextEncounter = areaToShow.GetComponent<AreaEncounter>();
            if (nextEncounter != null) nextEncounter.BeginEncounter();
        }

        onSwap?.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        if (overlay == null || fadeDuration <= 0f)
        {
            if (overlay != null) overlay.alpha = to;
            yield break;
        }
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        overlay.alpha = to;
    }
}
