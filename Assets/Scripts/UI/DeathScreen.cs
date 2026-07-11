using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================
// ECHOFORM — DeathScreen
// Watches a CombatManager; when it reaches Lose (Vestige dies), it waits a
// beat so the death animation can play, then FADES a "You died" panel to
// black. The Next button loads the Main Menu.
//
// Mirrors WalkOffOnVictory: it self-subscribes to OnStateChanged, so nothing
// needs wiring on CombatManager. Assign the CombatManager, the panel root and
// the Main Menu scene name; hook the Next button's OnClick to OnNextPressed().
//
// The fade uses a CanvasGroup on the panel. Put a full-screen black Image plus
// the "You Died" text inside the panel, add a CanvasGroup to the panel, and it
// all fades in together. Uses unscaled time so it works even if the game pauses.
// =====================================================

public class DeathScreen : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("The encounter to watch. When it reaches Lose, the screen appears.")]
    [SerializeField] private CombatManager combat;

    [Header("UI")]
    [Tooltip("The 'You died' panel (black background + text). Hidden on Awake, shown on death.")]
    [SerializeField] private GameObject root;
    [Tooltip("CanvasGroup on the panel — its alpha is faded 0 -> 1. Auto-found on root if left empty.")]
    [SerializeField] private CanvasGroup fadeGroup;
    [Tooltip("Seconds to wait after death before the fade starts, so the death animation can play.")]
    [SerializeField] private float showDelay = 1.5f;
    [Tooltip("Seconds for the fade-to-black. 0 = appear instantly.")]
    [SerializeField] private float fadeDuration = 0.75f;

    [Header("Next")]
    [Tooltip("Scene loaded when the player presses Next. Must be in Build Settings.")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool shown;

    void Awake()
    {
        if (root != null)
        {
            if (fadeGroup == null) fadeGroup = root.GetComponent<CanvasGroup>();
            if (fadeGroup != null) fadeGroup.alpha = 0f;
            root.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (combat != null)
        {
            combat.OnStateChanged += OnCombatState;
            if (combat.State == CombatState.Lose) Trigger();   // already lost before this activated
        }
    }

    void OnDisable()
    {
        if (combat != null) combat.OnStateChanged -= OnCombatState;
    }

    private void OnCombatState(CombatState s)
    {
        if (s == CombatState.Lose) Trigger();
    }

    private void Trigger()
    {
        if (shown) return;
        shown = true;
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (showDelay > 0f) yield return new WaitForSecondsRealtime(showDelay);
        if (root == null) yield break;

        root.SetActive(true);

        if (fadeGroup == null)
            yield break;                                   // no CanvasGroup: just pops on

        float t = 0f;
        fadeGroup.alpha = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = fadeDuration > 0f ? Mathf.Clamp01(t / fadeDuration) : 1f;
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }

    // Hook this to the Next button's OnClick (also callable from code).
    public void OnNextPressed()
    {
        if (string.IsNullOrEmpty(mainMenuScene))
        {
            Debug.LogError("[Echoform] DeathScreen: no Main Menu scene set.");
            return;
        }
        Time.timeScale = 1f;   // in case anything paused on death
        SceneManager.LoadScene(mainMenuScene);
    }
}
