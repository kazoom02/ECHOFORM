using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — CombatTargeting
// Runs the "pick an enemy" step for single-target chips.
// While active it raycasts the mouse against enemy colliders,
// shows the hover border, and reports the click. Right-click
// or Escape cancels. Works with either the legacy Input
// Manager or the new Input System package.
// =====================================================

public class CombatTargeting : MonoBehaviour
{
    [SerializeField] private Camera cam;   // defaults to Camera.main

    public bool IsTargeting { get; private set; }

    Action<Enemy> onPicked;
    Action onCancel;
    EnemySelectable hovered;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    /// <summary>Enter targeting mode. onPicked fires with the chosen enemy; onCancel if the player backs out.</summary>
    public void Begin(Action<Enemy> picked, Action cancelled = null)
    {
        onPicked = picked;
        onCancel = cancelled;
        IsTargeting = true;
    }

    void Update()
    {
        if (!IsTargeting) return;
        if (cam == null) cam = Camera.main;

        // cancel with right-click or Escape
        if (CancelPressed())
        {
            var cancel = onCancel;
            End();
            cancel?.Invoke();
            return;
        }

        // hover: raycast the mouse into the 2D world
        EnemySelectable sel = null;
        if (cam != null)
        {
            Vector3 sp = MouseScreenPos();
            Vector2 world = cam.ScreenToWorldPoint(sp);
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit != null) sel = hit.GetComponentInParent<EnemySelectable>();
            if (sel != null && (sel.Enemy == null || sel.Enemy.IsDead)) sel = null;
        }

        if (sel != hovered)
        {
            if (hovered) hovered.SetHighlight(false);
            hovered = sel;
            if (hovered) hovered.SetHighlight(true);
        }

        // confirm with left-click on a highlighted enemy
        if (hovered != null && ConfirmPressed())
        {
            Enemy picked = hovered.Enemy;
            var cb = onPicked;
            End();
            cb?.Invoke(picked);
        }
    }

    void End()
    {
        IsTargeting = false;
        if (hovered) hovered.SetHighlight(false);
        hovered = null;
        onPicked = null;
        onCancel = null;
    }

    // ---- input abstraction (works with either backend) ----
    static Vector3 MouseScreenPos()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool rmb = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        return rmb || esc;
#else
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
