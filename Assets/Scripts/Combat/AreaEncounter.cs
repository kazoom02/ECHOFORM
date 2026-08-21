using System.Collections.Generic;
using UnityEngine;

// =====================================================
// ECHOFORM — AreaEncounter
// Configura os inimigos e o ponto de controlo de cada área e inicia
// o encontro correspondente no gestor de combate partilhado.
// =====================================================

public class AreaEncounter : MonoBehaviour
{
    private const string Area1ObjectName = "Area1";
    private const string Area2ObjectName = "Area2";
    private const string Area2TransitionName = "Transition_2to3";
    private const string Area2ExitName = "Area2 Exit Square";

    [Tooltip("The shared CombatManager used by all areas in this scene.")]
    [SerializeField] private CombatManager combat;

    [Tooltip("One entry per starting enemy. Prefab entries are instantiated under EnemyRow; scene enemies are used in place.")]
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();

    [Tooltip("Optional formation anchor for this area. Leave empty to use CombatManager's shared EnemyRow.")]
    [SerializeField] private Transform enemyRowOverride;

    [Header("Checkpoint")]
    [Tooltip("Zero-based order used by Load Game: Area 1 = 0, Area 2 = 1, Area 3 = 2.")]
    [SerializeField] private int checkpointIndex;
    [SerializeField] private string checkpointName = "Area Checkpoint";
    [Tooltip("Fallback player position for older saves that do not contain a position.")]
    [SerializeField] private Vector3 checkpointSpawnPosition;

    public IReadOnlyList<Enemy> Enemies => enemies;
    public int CheckpointIndex => checkpointIndex;
    public string CheckpointName => checkpointName;
    public Vector3 CheckpointSpawnPosition => checkpointSpawnPosition;

    private void OnEnable()
    {
        string areaName = name.Trim();
        if (string.Equals(areaName, Area1ObjectName, System.StringComparison.OrdinalIgnoreCase))
            EnsureArea1ExitGate();
        else if (string.Equals(areaName, Area2ObjectName, System.StringComparison.OrdinalIgnoreCase))
            EnsureArea2VictoryExit();
    }

    private void EnsureArea1ExitGate()
    {
        ExitTrigger area1Exit = FindExitTemplate();
        if (area1Exit == null) return;

        GameObject area1 = null;
        AreaEncounter[] encounters = Resources.FindObjectsOfTypeAll<AreaEncounter>();
        foreach (AreaEncounter candidate in encounters)
        {
            if (candidate.gameObject.scene != gameObject.scene) continue;
            if (!string.Equals(candidate.name.Trim(), Area1ObjectName, System.StringComparison.OrdinalIgnoreCase)) continue;
            area1 = candidate.gameObject;
            break;
        }

        if (area1 != null) area1Exit.SetActiveAreaGate(area1);
    }

    private void EnsureArea2VictoryExit()
    {

        EnsureArea1ExitGate();

        if (combat == null)
            combat = FindAnyObjectByType<CombatManager>();

        AreaTransition transition = null;
        AreaTransition[] transitions = Resources.FindObjectsOfTypeAll<AreaTransition>();
        foreach (AreaTransition candidate in transitions)
        {
            if (candidate.gameObject.scene != gameObject.scene) continue;
            if (!string.Equals(candidate.name.Trim(), Area2TransitionName, System.StringComparison.OrdinalIgnoreCase)) continue;
            transition = candidate;
            break;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (combat == null || transition == null || player == null)
        {
            Debug.LogWarning("[AreaEncounter] Area2 victory exit could not be configured. Check CombatManager, Transition_2to3, and the Player tag.", this);
            return;
        }

        Transform exit = transform.Find(Area2ExitName);
        if (exit == null)
        {
            GameObject exitObject = new GameObject(Area2ExitName);
            exit = exitObject.transform;
            exit.SetParent(transform, false);

            ExitTrigger template = FindExitTemplate();
            BoxCollider2D box = exitObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            if (template != null)
            {
                exit.position = template.transform.position;
                exit.rotation = template.transform.rotation;
                exit.localScale = template.transform.lossyScale;

                BoxCollider2D templateBox = template.GetComponent<BoxCollider2D>();
                if (templateBox != null)
                {
                    box.size = templateBox.size;
                    box.offset = templateBox.offset;
                }
            }
            else
            {
                exit.localPosition = new Vector3(1.6f, -3.5143f, 0f);
                exit.localScale = new Vector3(1f, 5.1685f, 1f);
            }
        }

        BoxCollider2D exitCollider = exit.GetComponent<BoxCollider2D>();
        if (exitCollider == null) exitCollider = exit.gameObject.AddComponent<BoxCollider2D>();
        exitCollider.isTrigger = true;

        ExitTrigger trigger = exit.GetComponent<ExitTrigger>();
        if (trigger == null) trigger = exit.gameObject.AddComponent<ExitTrigger>();
        trigger.Configure(combat, transition, "Player", gameObject);

        WalkOffOnVictory walkOff = GetComponent<WalkOffOnVictory>();
        if (walkOff == null) walkOff = gameObject.AddComponent<WalkOffOnVictory>();
        walkOff.Configure(combat, player.transform, exit);

        Debug.Log("[AreaEncounter] Area2 victory exit is ready for Transition_2to3.", this);
    }

    private ExitTrigger FindExitTemplate()
    {
        ExitTrigger[] exits = Resources.FindObjectsOfTypeAll<ExitTrigger>();
        foreach (ExitTrigger candidate in exits)
        {
            if (candidate.gameObject.scene != gameObject.scene) continue;
            if (candidate.transform.IsChildOf(transform)) continue;
            if (candidate.GetComponent<BoxCollider2D>() != null) return candidate;
        }

        return null;
    }

    public void BeginEncounter()
    {
        if (combat == null)
            combat = FindAnyObjectByType<CombatManager>();

        if (combat != null)
        {
            combat.SaveCheckpoint(checkpointIndex, checkpointName);
            combat.StartCombat(enemies, enemyRowOverride);
        }
        else
            Debug.LogWarning($"[AreaEncounter] {name} cannot start because no CombatManager was found.", this);
    }
}
