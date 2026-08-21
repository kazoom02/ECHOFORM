using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// =====================================================
// ECHOFORM — AttackAnimTester
// Ferramenta de teste que reproduz isoladamente uma animação de ataque
// através de um PlayableGraph e regressa depois à animação de repouso.
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
