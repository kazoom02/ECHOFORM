using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;

// =====================================================
// ECHOFORM — AreaTransition
// Plays a transition video IN-SCENE (no scene load) to cover the cut
// from one area to the next. Flow when you call Play():
//   1. Fade a fullscreen overlay (the video) in.
//   2. Play the clip through.
//   3. While the screen is covered, swap Area1 -> Area2
//      (and optionally teleport the player/camera).
//   4. Fade the overlay out to reveal Area2.
//
// Setup: a VideoPlayer (Render Mode = Render Texture) pointing at a
// RenderTexture, shown on a fullscreen RawImage whose CanvasGroup is
// wired below. Call Play() from your "slimes cleared + walked off"
// trigger. See the header notes in chat for exact wiring.
// =====================================================

public class AreaTransition : MonoBehaviour
{
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

    [Header("Events")]
    [Tooltip("Fired when the transition begins (e.g. disable player input).")]
    public UnityEvent onTransitionStarted;
    [Tooltip("Fired at the swap point, while the video hides the cut.")]
    public UnityEvent onSwap;
    [Tooltip("Fired when the overlay has faded out and Area2 is visible (e.g. re-enable input).")]
    public UnityEvent onTransitionFinished;

    private bool playing;

    void Awake()
    {
        // Never play on scene load — otherwise the clip's audio plays in the
        // background before the transition is triggered. Start silent + hidden.
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

        // Draw the video on top WITHOUT modifying the parent/shared canvas.
        // (Flipping a shared canvas's render mode shoves the rest of the UI —
        // e.g. the health frame — off-screen.) A nested Canvas on the overlay's
        // OWN object with overrideSorting lifts only the video.
        if (forceOverlayOnTop && overlay != null)
        {
            Canvas oc = overlay.GetComponent<Canvas>();
            if (oc == null) oc = overlay.gameObject.AddComponent<Canvas>();
            oc.overrideSorting = true;
            oc.sortingOrder = overlaySortOrder;
        }

        EnsureVideoWiring();
    }

    // Guarantees the video is actually visible: RawImage -> VideoPlayer's
    // RenderTexture, VideoPlayer in Render-Texture mode, RawImage stretched
    // fullscreen and opaque. Fixes the usual "transition happens but no video"
    // wiring mistakes without touching the scene.
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
            videoImage.color = Color.white;                 // not tinted transparent
            RectTransform rt = videoImage.rectTransform;    // stretch fullscreen
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

    // Call this to start the transition. Safe to spam — ignores re-entry.
    public void Play()
    {
        if (playing) return;
        playing = true;
        gameObject.SetActive(true);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        onTransitionStarted?.Invoke();

        // 1) Cover the screen with the video overlay.
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.blocksRaycasts = true;
            yield return Fade(0f, 1f);
        }

        // 2) Play the clip; swap the areas one frame in (screen already covered).
        if (videoPlayer != null)
        {
            bool done = false;
            void OnEnd(VideoPlayer vp) => done = true;

            if (videoPlayer.clip == null && videoPlayer.source == VideoSource.VideoClip)
                Debug.LogWarning("[AreaTransition] VideoPlayer has no clip assigned — nothing will show.", this);

            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnEnd;
            videoPlayer.Play();

            yield return null;   // let the first frame appear before we cut
            DoSwap();

            while (!done) yield return null;
            videoPlayer.loopPointReached -= OnEnd;
        }
        else
        {
            DoSwap();   // no clip assigned — still do the swap
        }

        // 3) Reveal Area2.
        if (overlay != null)
        {
            yield return Fade(1f, 0f);
            overlay.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
        }

        onTransitionFinished?.Invoke();
        playing = false;
    }

    private void DoSwap()
    {
        if (areaToHide != null) areaToHide.SetActive(false);
        if (areaToShow != null) areaToShow.SetActive(true);
        if (objectToTeleport != null && teleportTarget != null)
            objectToTeleport.position = teleportTarget.position;
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
            t += Time.unscaledDeltaTime;   // works even if you pause/time-scale
            overlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        overlay.alpha = to;
    }
}
