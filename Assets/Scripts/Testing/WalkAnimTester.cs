using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// =====================================================
// ECHOFORM — WalkAnimTester
// Ferramenta de teste que inicia e interrompe em repetição uma animação
// de caminhada através de um PlayableGraph.
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
