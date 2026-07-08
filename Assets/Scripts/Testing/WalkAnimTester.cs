using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// =====================================================
// ECHOFORM — WalkAnimTester  (TEMPORARY / debug only)
// A throwaway toggle button to preview the WALK animation.
// Walking loops, so this is a start/stop toggle (unlike the
// one-shot AttackAnimTester).
//
// SETUP:
//   1. Add this component to the character GameObject
//      (the one with the Animator). It can sit alongside
//      AttackAnimTester on the same object.
//   2. Drag the Walking clip into "Walk Clip".
//   3. Press Play → click "▶ Start Walk" (or press W) to loop
//      it; click again / press W to stop and return to Idle.
//
// Plays the clip through a PlayableGraph, so it ignores your
// Animator Controller. Delete before shipping. StartWalk() /
// StopWalk() are public so you can hook them to UI Buttons.
// =====================================================

[RequireComponent(typeof(Animator))]
public class WalkAnimTester : MonoBehaviour
{
    [Header("Assign the walk clip (Walking)")]
    [SerializeField] private AnimationClip walkClip;

    [Header("Options")]
    [SerializeField] private bool wKeyAlsoToggles = true;
    [SerializeField, Range(0.25f, 3f)] private float playbackSpeed = 1f;
    [SerializeField] private bool showOnScreenButton = true;

    private Animator animator;
    private PlayableGraph graph;
    private AnimationClipPlayable clipPlayable;
    private bool walking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (wKeyAlsoToggles && WPressedThisFrame())
            Toggle();

        // Manual loop so it works even if the clip has Loop Time disabled.
        if (walking && walkClip != null && clipPlayable.IsValid())
        {
            double t = clipPlayable.GetTime();
            if (t >= walkClip.length)
                clipPlayable.SetTime(t % walkClip.length);
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenButton) return;

        var style = new GUIStyle(GUI.skin.button) { fontSize = 20 };
        string label = walkClip == null ? "Assign a clip!" : (walking ? "■ Stop Walk" : "▶ Start Walk");
        // sits just below AttackAnimTester's button (which is at y=20)
        if (GUI.Button(new Rect(20, 90, 230, 60), label, style))
            Toggle();
    }

    public void Toggle()
    {
        if (walking) StopWalk();
        else StartWalk();
    }

    public void StartWalk()
    {
        if (walkClip == null)
        {
            Debug.LogWarning("[WalkAnimTester] No walk clip assigned.");
            return;
        }
        if (walking) return;

        StopGraph();

        graph = PlayableGraph.Create("WalkTester");
        var output = AnimationPlayableOutput.Create(graph, "Anim", animator);
        clipPlayable = AnimationClipPlayable.Create(graph, walkClip);
        clipPlayable.SetSpeed(playbackSpeed);
        output.SetSourcePlayable(clipPlayable);

        graph.Play();
        walking = true;
    }

    public void StopWalk()
    {
        StopGraph();
        walking = false;

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
        StopWalk();
    }

    // Input-backend agnostic (new Input System, legacy, or both).
    private static bool WPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.wKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.W);
#else
        return false;
#endif
    }
}
