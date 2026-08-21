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
// Reproduz uma sequência de vídeos com páginas de texto, processa os
// comandos de avanço e carrega a cena seguinte no fim.
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

    private void AutoAdvancePages()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying) return;

        Segment seg = segments[segmentIndex];
        int pageCount = (seg.pages != null) ? seg.pages.Length : 0;
        if (pageCount <= 1) return;

        double length = videoPlayer.length;
        if (length <= 0.0) return;

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

    private bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;

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
            videoPlayer.isLooping = false;
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

    private void OnVideoEnd(VideoPlayer source)
    {
        if (finished) return;

        Segment seg = segments[segmentIndex];
        bool onLastPage = seg.pages == null || seg.pages.Length == 0 || pageIndex >= seg.pages.Length - 1;
        if (onLastPage) Advance();
    }

        public void Advance()
    {
        if (finished) return;

        Segment seg = segments[segmentIndex];

        if (seg.pages != null && pageIndex + 1 < seg.pages.Length)
        {
            pageIndex++;
            ShowPage();
            return;
        }

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
