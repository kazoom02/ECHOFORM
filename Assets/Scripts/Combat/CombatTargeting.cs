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
    float nextNavigationTime;
    Vector3 lastMouseScreenPos;
    bool hasMouseScreenPos;

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
        hasMouseScreenPos = true;
        lastMouseScreenPos = MouseScreenPos();
        SelectControllerTarget(FindDefaultSelectable());
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

        int direction = ReadNavigationDirection();
        if (direction != 0)
            CycleControllerTarget(direction);

        // hover: raycast the mouse into the 2D world when the mouse is being used
        if (cam != null && MouseMovedOrClicked())
        {
            EnemySelectable sel = null;
            Vector3 sp = MouseScreenPos();
            Vector2 world = cam.ScreenToWorldPoint(sp);
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit != null) sel = hit.GetComponentInParent<EnemySelectable>();
            if (sel != null && (sel.Enemy == null || sel.Enemy.IsDead)) sel = null;

            SelectControllerTarget(sel);
        }

        // confirm with left-click or gamepad south button on a highlighted enemy
        if (hovered != null && ConfirmPressed())
        {
            Enemy picked = hovered.Enemy;
            var cb = onPicked;
            End();
            cb?.Invoke(picked);
        }
    }

    /// <summary>Cancel targeting from outside (e.g. the player clicked a different chip).
    /// Fires the same onCancel callback as a right-click / Escape.</summary>
    public void Cancel()
    {
        if (!IsTargeting) return;
        var cancel = onCancel;
        End();
        cancel?.Invoke();
    }

    void End()
    {
        IsTargeting = false;
        if (hovered) hovered.SetHighlight(false);
        hovered = null;
        onPicked = null;
        onCancel = null;
    }

    void SelectControllerTarget(EnemySelectable target)
    {
        if (target == hovered) return;

        if (hovered) hovered.SetHighlight(false);
        hovered = target;
        if (hovered) hovered.SetHighlight(true);
    }

    void CycleControllerTarget(int direction)
    {
        EnemySelectable[] targets = FindSelectableTargets();
        if (targets.Length == 0)
        {
            SelectControllerTarget(null);
            return;
        }

        int index = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == hovered)
            {
                index = i;
                break;
            }
        }

        index = Mod(index + direction, targets.Length);
        SelectControllerTarget(targets[index]);
    }

    EnemySelectable FindDefaultSelectable()
    {
        EnemySelectable[] targets = FindSelectableTargets();
        return targets.Length > 0 ? targets[0] : null;
    }

    static EnemySelectable[] FindSelectableTargets()
    {
        EnemySelectable[] all = FindObjectsOfType<EnemySelectable>();
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Enemy enemy = all[i] != null ? all[i].Enemy : null;
            if (enemy != null && !enemy.IsDead) count++;
        }

        EnemySelectable[] targets = new EnemySelectable[count];
        int targetIndex = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Enemy enemy = all[i] != null ? all[i].Enemy : null;
            if (enemy != null && !enemy.IsDead)
                targets[targetIndex++] = all[i];
        }

        Array.Sort(targets, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        return targets;
    }

    static int Mod(int value, int count)
    {
        if (count <= 0) return 0;
        int result = value % count;
        return result < 0 ? result + count : result;
    }

    // ---- input abstraction (works with either backend) ----
    bool MouseMovedOrClicked()
    {
        Vector3 current = MouseScreenPos();
        bool moved = !hasMouseScreenPos || (current - lastMouseScreenPos).sqrMagnitude > 0.01f;
        hasMouseScreenPos = true;
        lastMouseScreenPos = current;
        return moved || MouseConfirmPressed();
    }

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
        bool mouse = MouseConfirmPressed();
        bool gamepad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool keyboard = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        return mouse || gamepad || keyboard;
#else
        return MouseConfirmPressed() || Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
    }

    static bool MouseConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    int ReadNavigationDirection()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame) return -1;
            if (kb.rightArrowKey.wasPressedThisFrame) return 1;
        }

        Gamepad pad = Gamepad.current;
        if (pad == null) return 0;

        if (pad.dpad.left.wasPressedThisFrame) return -1;
        if (pad.dpad.right.wasPressedThisFrame) return 1;

        float x = pad.leftStick.x.ReadValue();
        if (Mathf.Abs(x) < 0.5f)
        {
            nextNavigationTime = 0f;
            return 0;
        }

        if (Time.unscaledTime < nextNavigationTime) return 0;
        nextNavigationTime = Time.unscaledTime + 0.18f;
        return x < 0f ? -1 : 1;
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow)) return -1;
        if (Input.GetKeyDown(KeyCode.RightArrow)) return 1;
        if (Input.GetAxisRaw("Horizontal") < -0.5f && Time.unscaledTime >= nextNavigationTime)
        {
            nextNavigationTime = Time.unscaledTime + 0.18f;
            return -1;
        }
        if (Input.GetAxisRaw("Horizontal") > 0.5f && Time.unscaledTime >= nextNavigationTime)
        {
            nextNavigationTime = Time.unscaledTime + 0.18f;
            return 1;
        }
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.5f) nextNavigationTime = 0f;
        return 0;
#endif
    }

    static bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool rmb = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepad = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        return rmb || esc || gamepad;
#else
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
#endif
    }
}
