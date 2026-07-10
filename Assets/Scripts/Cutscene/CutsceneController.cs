using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — CutsceneController
// Plays a sequence of videos. Over each video one or more
// text "pages" are shown. The player advances with the
// on-screen Continue button, the gamepad south button
// (A on Xbox / Cross on PlayStation), or Space/Enter.
//
//   Advance rules:
//     - more pages left on this video?  -> show next page
//     - otherwise more videos left?     -> play next video (page 0)
//     - otherwise                       -> load 'nextScene'
//
// Build the scene with Tools > ECHOFORM > Build Scenematic Scene,
// then assign your 8 clips + their text pages on this component.
// =====================================================

public class CutsceneController : MonoBehaviour
{
    [Serializable]
    public class Segment
    {
        [Tooltip("The video clip for this step of the sequence.")]
        public VideoClip clip;

        [Tooltip("One entry per text page shown over this clip. Leave empty for a silent clip.")]
        [TextArea(2, 5)] public string[] pages;
    }

    [Header("Sequence")]
    [Tooltip("Your videos in order. Each has its own list of text pages.")]
    [SerializeField] private Segment[] segments;

    [Tooltip("Scene loaded after the last page of the last video.")]
    [SerializeField] private string nextScene = "FirstArea";

    [Header("References (auto-wired by the builder)")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private Button continueButton;

    private int segmentIndex = -1;
    private int pageIndex = 0;
    private bool finished = false;

    void Awake()
    {
        if (continueButton != null) continueButton.onClick.AddListener(Advance);
        if (videoPlayer != null) videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Advance);
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoEnd;
    }

    void Start()
    {
        if (segments == null || segments.Length == 0)
        {
            Debug.LogWarning("[Scenematic] No segments assigned — nothing to play.");
            Finish();
            return;
        }
        PlaySegment(0);
    }

    void Update()
    {
        if (finished) return;

        if (ConfirmPressed()) { Advance(); return; }

        AutoAdvancePages();
    }

    // Spreads a clip's text pages evenly across the video's length so they all
    // appear before it ends. Only walks the pages forward — the video's end event
    // handles moving on to the next clip. Manual advances still take priority.
    private void AutoAdvancePages()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying) return;

        Segment seg = segments[segmentIndex];
        int pageCount = (seg.pages != null) ? seg.pages.Length : 0;
        if (pageCount <= 1) return;

        double length = videoPlayer.length;
        if (length <= 0.0) return;                 // not known until the clip is prepared

        double perPage = length / pageCount;
        int target = pageIndex;
        while (target < pageCount - 1 && videoPlayer.time >= (target + 1) * perPage)
            target++;

        if (target != pageIndex)
        {
            pageIndex = target;
            ShowPage();
        }
    }

    // True on the frame the player presses a "confirm/continue" input.
    private bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true; // A (Xbox) / Cross (PS)

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;

        return false;
#else
        return Input.GetButtonDown("Submit");
#endif
    }

    private void PlaySegment(int index)
    {
        segmentIndex = index;
        pageIndex = 0;

        Segment seg = segments[index];

        if (videoPlayer != null && seg.clip != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = seg.clip;
            videoPlayer.isLooping = false;   // play once, then hold the last frame until the player advances
            videoPlayer.Play();
        }

        ShowPage();
    }

    private void ShowPage()
    {
        Segment seg = segments[segmentIndex];
        string page = (seg.pages != null && pageIndex < seg.pages.Length) ? seg.pages[pageIndex] : string.Empty;
        if (captionText != null) captionText.text = page;
    }

    // Fires when a (non-looping) video reaches its end. Auto-advances only when the
    // last text page for this clip is already showing, so short clips never skip unread text.
    private void OnVideoEnd(VideoPlayer source)
    {
        if (finished) return;

        Segment seg = segments[segmentIndex];
        bool onLastPage = seg.pages == null || seg.pages.Length == 0 || pageIndex >= seg.pages.Length - 1;
        if (onLastPage) Advance();
    }

    /// <summary>Advance the sequence. Also hooked to the Continue button's OnClick.</summary>
    public void Advance()
    {
        if (finished) return;

        Segment seg = segments[segmentIndex];

        // Another text page on the current video?
        if (seg.pages != null && pageIndex + 1 < seg.pages.Length)
        {
            pageIndex++;
            ShowPage();
            return;
        }

        // Another video in the sequence?
        if (segmentIndex + 1 < segments.Length)
        {
            PlaySegment(segmentIndex + 1);
            return;
        }

        Finish();
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        if (videoPlayer != null) videoPlayer.Stop();

        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
    }
}
