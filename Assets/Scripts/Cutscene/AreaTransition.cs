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
