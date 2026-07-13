using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Animates scene-owned credits UI. All presentation and text live in the
/// Credits scene; this component only moves/fades the assigned RectTransform.
/// </summary>
public sealed class CreditsScreen : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform creditsText;
    [SerializeField] private CanvasGroup creditsGroup;

    [Header("Bottom-to-Top Crawl")]
    [Tooltip("Position below the bottom edge where the credits begin.")]
    [SerializeField] private float bottomY = -1250f;
    [Tooltip("Position above the top edge where the credits finish.")]
    [SerializeField] private float topY = 1250f;
    [SerializeField, Min(1f)] private float scrollDuration = 36f;
    [SerializeField, Min(0f)] private float startDelay = 1.25f;
    [SerializeField, Min(0f)] private float endHold = 2.5f;
    [SerializeField, Range(0.01f, 0.25f)] private float edgeFade = 0.08f;
    [SerializeField] private float startScale = 1.05f;
    [SerializeField] private float endScale = 0.72f;

    [Header("Navigation")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private bool returnToMenuWhenFinished = true;
    [SerializeField, Min(0f)] private float inputDelay = 1f;

    private float elapsed;
    private bool leaving;

    private void OnEnable()
    {
        elapsed = 0f;
        leaving = false;
        ApplyCrawlPosition(0f);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (leaving)
            return;

        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= inputDelay && ReturnPressed())
        {
            ReturnToMainMenu();
            return;
        }

        float crawlTime = elapsed - startDelay;
        if (crawlTime < 0f)
        {
            ApplyCrawlPosition(0f);
            return;
        }

        float progress = Mathf.Clamp01(crawlTime / Mathf.Max(1f, scrollDuration));
        ApplyCrawlPosition(progress);

        if (returnToMenuWhenFinished && crawlTime >= scrollDuration + endHold)
            ReturnToMainMenu();
    }

    private void ApplyCrawlPosition(float progress)
    {
        if (creditsText != null)
        {
            Vector2 position = creditsText.anchoredPosition;
            // Resolve the smaller value as the bottom and the larger value as
            // the top, then always travel upward even if Inspector values are
            // accidentally entered in reverse.
            float resolvedTop = Mathf.Max(topY, bottomY);
            float resolvedBottom = Mathf.Min(topY, bottomY);
            position.y = Mathf.LerpUnclamped(resolvedBottom, resolvedTop, SmoothStep(progress));
            creditsText.anchoredPosition = position;

            float scale = Mathf.Lerp(startScale, endScale, progress);
            creditsText.localScale = new Vector3(scale, scale, 1f);
        }

        if (creditsGroup != null)
        {
            float fadeIn = Mathf.Clamp01(progress / edgeFade);
            float fadeOut = Mathf.Clamp01((1f - progress) / edgeFade);
            creditsGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(fadeIn, fadeOut));
        }
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private void ReturnToMainMenu()
    {
        if (leaving || string.IsNullOrWhiteSpace(mainMenuScene))
            return;

        leaving = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    private static bool ReturnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null &&
               (gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel");
#endif
    }
}
