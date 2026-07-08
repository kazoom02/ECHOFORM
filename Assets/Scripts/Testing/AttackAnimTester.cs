using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// =====================================================
// ECHOFORM — AttackAnimTester  (TEMPORARY / debug only)
// A throwaway button to preview the attack animation without
// setting up states, triggers or transitions in the Animator
// Controller.
//
// SETUP (10 seconds):
//   1. Add this component to the character GameObject
//      (the one with the Animator).
//   2. Drag the Attack1 clip into "Attack Clip".
//   3. Press Play → click the on-screen "▶ Test Attack"
//      button (or press Space). It plays the clip once and
//      returns to Idle.
//
// It plays the clip through a PlayableGraph, so it doesn't
// touch your controller. Delete this script before shipping.
// The PlayAttack() method is public, so you can also hook it
// to a real UI Button's OnClick instead of the on-screen one.
// =====================================================

[RequireComponent(typeof(Animator))]
public class AttackAnimTester : MonoBehaviour
{
    [Header("Assign the attack clip (Attack1)")]
    [SerializeField] private AnimationClip attackClip;

    [Header("Options")]
    [SerializeField] private bool spaceKeyAlsoTriggers = true;
    [SerializeField, Range(0.25f, 3f)] private float playbackSpeed = 1f;
    [SerializeField] private bool showOnScreenButton = true;

    private Animator animator;
    private PlayableGraph graph;
    private bool playing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (spaceKeyAlsoTriggers && SpacePressedThisFrame())
            PlayAttack();
    }

    // Works whether the project uses the new Input System, the legacy
    // Input Manager, or both (set in Project Settings ▸ Player ▸ Active Input Handling).
    private static bool SpacePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private void OnGUI()
    {
        if (!showOnScreenButton) return;

        var style = new GUIStyle(GUI.skin.button) { fontSize = 20 };
        string label = attackClip == null ? "Assign a clip!" : (playing ? "Playing..." : "▶ Test Attack");
        if (GUI.Button(new Rect(20, 20, 230, 60), label, style))
            PlayAttack();
    }

    /// <summary>Play the assigned attack clip once, then hand control back to the controller.</summary>
    public void PlayAttack()
    {
        if (attackClip == null)
        {
            Debug.LogWarning("[AttackAnimTester] No attack clip assigned.");
            return;
        }
        if (playing) return;

        StopGraph();

        graph = PlayableGraph.Create("AttackTester");
        var output = AnimationPlayableOutput.Create(graph, "Anim", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, attackClip);
        clipPlayable.SetSpeed(playbackSpeed);
        output.SetSourcePlayable(clipPlayable);

        graph.Play();
        playing = true;

        float duration = attackClip.length / Mathf.Max(0.01f, playbackSpeed);
        StartCoroutine(EndAfter(duration));
    }

    private IEnumerator EndAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopGraph();
        playing = false;

        // Return to the Animator Controller (back to Idle).
        if (animator.runtimeAnimatorController != null)
            animator.Rebind();
    }

    private void StopGraph()
    {
        if (graph.IsValid())
            graph.Destroy();
    }

    private void OnDisable()
    {
        StopGraph();
    }
}
