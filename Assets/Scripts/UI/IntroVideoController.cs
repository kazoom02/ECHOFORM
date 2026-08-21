using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

// =====================================================
// ECHOFORM — IntroVideoController
// Reproduz o vídeo de introdução, permite ignorá-lo e carrega a primeira
// área quando a reprodução termina.
// =====================================================

public class IntroVideoController : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Scene loaded when the video ends or is skipped.")]
    [SerializeField] private string nextSceneName = "FirstArea";

    [Header("Skip")]
    [SerializeField] private Button skipButton;

    private bool advancing;

    void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("[Echoform] IntroVideoController: no VideoPlayer assigned — going straight to the game.");
            Advance();
            return;
        }

        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();

        if (skipButton != null) skipButton.onClick.AddListener(Skip);
    }

    void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
    }

    public void Skip() => Advance();

    private void OnVideoFinished(VideoPlayer vp) => Advance();

    private void Advance()
    {
        if (advancing) return;
        advancing = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
