using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — EndTurnButton
// Ends the player's turn (enemies then act). Auto-disables
// while it isn't the player's turn so you can't spam it or
// act during the enemy phase. Put on a UI Button.
// =====================================================

[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private CombatManager combat;

    Button button;

    void Awake() { button = GetComponent<Button>(); }

    void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        if (combat != null) combat.OnStateChanged += OnStateChanged;
        Refresh();
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        if (combat != null) combat.OnStateChanged -= OnStateChanged;
    }

    void OnClick()
    {
        if (combat != null) combat.EndTurn();
    }

    void OnStateChanged(CombatState state) => Refresh();

    void Refresh()
    {
        if (button != null)
            button.interactable = combat != null && combat.State == CombatState.PlayerTurn;
    }
}
