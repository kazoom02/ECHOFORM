using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

// =====================================================
// ECHOFORM — IntroVideoController
// Plays the lore intro video, then loads Area 1.
//   - Skip button (kept): player can jump to the game early.
//   - Auto-advance: when the video reaches its end, it loads
//     the next scene on its own — no click needed.
// Put this in the Intro scene alongside a VideoPlayer and a
// Skip UI Button. Add the Intro scene to Build Settings.
// =====================================================

public class IntroVideoController : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Scene loaded when the video ends or is skipped.")]
    [SerializeField] private string nextSceneName = "FirstArea";

    [Header("Skip")]
    [SerializeField] private Button skipButton;

    private bool advancing;   // guards against loading twice

    void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("[Echoform] IntroVideoController: no VideoPlayer assigned — going straight to the game.");
            Advance();
            return;
        }

        videoPlayer.isLooping = false;                 // must not loop, or it never ends
        videoPlayer.loopPointReached += OnVideoFinished; // fires when the clip finishes
        videoPlayer.Play();

        if (skipButton != null) skipButton.onClick.AddListener(Skip);
    }

    void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
    }

    /// <summary>Skip button handler (also assignable from the Inspector OnClick).</summary>
    public void Skip() => Advance();

    private void OnVideoFinished(VideoPlayer vp) => Advance();

    private void Advance()
    {
        if (advancing) return;   // e.g. skip pressed on the final frame
        advancing = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
