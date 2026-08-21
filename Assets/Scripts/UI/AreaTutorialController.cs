using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — AreaTutorialController
// Conduz o tutorial da primeira área, acompanhando o estado do combate
// e apresentando instruções e realces sobre os elementos relevantes.
// =====================================================

public class AreaTutorialController : MonoBehaviour
{
    enum Step
    {
        PlayFirstChip,
        EndFirstTurn,
        EnemyTurn,
        WaitForCorruption,
        CorruptionWarning,
        Complete
    }

    enum CardSide
    {
        Left,
        Right
    }

    [Header("Scene references")]
    [SerializeField] private CombatManager combat;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform firstChipAnchor;
    [SerializeField] private RectTransform endTurnAnchor;
    [SerializeField] private InGameSettingsPanelController settingsPanel;
    [SerializeField] private NeuralInterfaceHUD neuralHud;

    [Header("Scene UI")]
    [SerializeField] private RectTransform overlay;
    [SerializeField] private RectTransform card;
    [SerializeField] private RectTransform highlight;
    [SerializeField] private Image[] highlightLines;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private CanvasGroup cardGroup;

    [Header("Tuning")]
    [Tooltip("Margin from the upper-left corner when the tutorial is shown left of the Neural Rack.")]
    [SerializeField] private Vector2 cardOffset = new Vector2(34f, -34f);
    [Tooltip("Margin from the upper-right corner when the tutorial is shown right of the Neural Rack.")]
    [SerializeField] private Vector2 rightCardOffset = new Vector2(-34f, -34f);
    [SerializeField] private float highlightPadding = 18f;
    [SerializeField] private float highlightThickness = 7f;
    [SerializeField] private float highlightPulseScale = 1.08f;
    [SerializeField] private float finalCardSeconds = 5f;

    RectTransform[] chipAnchors;
    RectTransform currentHighlightTarget;
    Step step;
    bool sawEnemyTurn;
    float completeTimer;

    void OnEnable()
    {
        ResolveReferences();
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
        }

        if (combat != null)
        {
            combat.OnCardPlayed += OnCardPlayed;
            combat.OnStateChanged += OnStateChanged;
            combat.OnHandCorrupted += OnHandCorrupted;
        }

        SetStep(Step.PlayFirstChip);
    }

    void OnDisable()
    {
        if (combat != null)
        {
            combat.OnCardPlayed -= OnCardPlayed;
            combat.OnStateChanged -= OnStateChanged;
            combat.OnHandCorrupted -= OnHandCorrupted;
        }

        if (overlay != null) overlay.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateOverlayVisibility();
        if (overlay == null || !overlay.gameObject.activeSelf) return;

        if (highlight != null && currentHighlightTarget != null)
        {
            highlight.gameObject.SetActive(!ShouldSuppressHighlight());
            if (!highlight.gameObject.activeSelf) return;

            FollowHighlightTarget();
            float pulse = 0.7f + Mathf.Sin(Time.unscaledTime * 5f) * 0.25f;
            highlight.localScale = Vector3.one * Mathf.Lerp(1f, highlightPulseScale, pulse);
            SetHighlightColor(new Color(0.25f, 1f, 1f, pulse));
        }

        if (step == Step.Complete && overlay != null)
        {
            completeTimer += Time.unscaledDeltaTime;
            float fadeStart = Mathf.Max(0.1f, finalCardSeconds - 1f);
            if (completeTimer >= fadeStart && cardGroup != null)
                cardGroup.alpha = Mathf.Clamp01(1f - (completeTimer - fadeStart));
            if (completeTimer >= finalCardSeconds)
            {
                overlay.gameObject.SetActive(false);
                enabled = false;
            }
        }
    }

    void ResolveReferences()
    {
        if (combat == null) combat = FindObjectOfType<CombatManager>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (settingsPanel == null) settingsPanel = FindObjectOfType<InGameSettingsPanelController>();
        if (neuralHud == null) neuralHud = FindObjectOfType<NeuralInterfaceHUD>();

        if (canvas == null) return;
        ResolveChipAnchors();
        if (firstChipAnchor == null && chipAnchors.Length > 0) firstChipAnchor = chipAnchors[0];
        if (endTurnAnchor == null) endTurnAnchor = FindRect(canvas.transform, "EndTurnButton");
    }

    void ResolveChipAnchors()
    {
        if (canvas == null)
        {
            chipAnchors = new RectTransform[0];
            return;
        }

        chipAnchors = new RectTransform[5];
        for (int i = 0; i < chipAnchors.Length; i++)
            chipAnchors[i] = FindRect(canvas.transform, "RackSlot_" + i);
    }

    static RectTransform FindRect(Transform root, string targetName)
    {
        foreach (RectTransform child in root.GetComponentsInChildren<RectTransform>(true))
            if (child.name == targetName) return child;
        return null;
    }

    void SetHighlightColor(Color color)
    {
        if (highlightLines == null) return;

        foreach (Image line in highlightLines)
            if (line != null) line.color = color;
    }

    void SetStep(Step next)
    {
        step = next;
        completeTimer = 0f;
        if (cardGroup != null) cardGroup.alpha = 1f;

        switch (step)
        {
            case Step.PlayFirstChip:
                PositionCard(CardSide.Left);
                SetCard("Memory Chips",
                    "Each card is a chip in the neural rack. Click one to install it, spend CPU cycles, and make Vestige act.",
                    "Objective: play any chip.");
                SetHighlight(firstChipAnchor);
                break;
            case Step.EndFirstTurn:
                PositionCard(CardSide.Right);
                SetCard("End The Turn",
                    "CPU pips are your action budget. When you are done installing chips, pass control and let the enemy move.",
                    "Objective: press End Turn when ready.");
                SetHighlight(endTurnAnchor);
                break;
            case Step.EnemyTurn:
                PositionCard(CardSide.Right);
                SetCard("Enemy Thread",
                    "Enemies act after your turn. Block protects this turn only, while shields can stop later hits.",
                    "Objective: survive the enemy turn.");
                SetHighlight(null);
                break;
            case Step.WaitForCorruption:
                PositionCard(CardSide.Right);
                SetCard("Keep Cutting",
                    "The Loom watches your pattern. On every third player turn, it copies one chip into corrupted memory.",
                    "Objective: play and end turns until the copy appears.");
                SetHighlight(null);
                break;
            case Step.CorruptionWarning:
                PositionCard(CardSide.Left);
                SetCard("The Loom Copies You",
                    "Every few turns, the Loom corrupts one of your chips. Corrupted chips cannot be installed and too many will overload memory.",
                    "Objective: watch the corrupted chip.");
                break;
            case Step.Complete:
                PositionCard(CardSide.Left);
                SetCard("Tutorial Complete",
                    "That is the loop: install chips, spend CPU wisely, end the turn, and watch the Loom's corrupted copies.",
                    "Area 1 training uploaded.");
                SetHighlight(null);
                break;
        }
    }

    void PositionCard(CardSide side)
    {
        if (card == null) return;

        bool onRight = side == CardSide.Right;
        float horizontalAnchor = onRight ? 1f : 0f;
        card.anchorMin = new Vector2(horizontalAnchor, 1f);
        card.anchorMax = new Vector2(horizontalAnchor, 1f);
        card.pivot = new Vector2(horizontalAnchor, 1f);
        card.anchoredPosition = onRight ? rightCardOffset : cardOffset;
    }

    void SetCard(string title, string body, string objective)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (objectiveText != null) objectiveText.text = objective;
    }

    void SetHighlight(RectTransform target)
    {
        currentHighlightTarget = target;
        if (highlight != null) highlight.gameObject.SetActive(target != null && !ShouldSuppressHighlight());
    }

    void UpdateOverlayVisibility()
    {
        if (overlay == null) return;
        bool visible = settingsPanel == null || !settingsPanel.IsOpen;
        if (overlay.gameObject.activeSelf != visible)
            overlay.gameObject.SetActive(visible);
    }

    bool ShouldSuppressHighlight()
    {
        if (settingsPanel != null && settingsPanel.IsOpen) return true;
        return neuralHud != null && neuralHud.IsControllerSelectionVisible;
    }

    void FollowHighlightTarget()
    {
        if (overlay == null || currentHighlightTarget == null) return;

        Vector3[] corners = new Vector3[4];
        currentHighlightTarget.GetWorldCorners(corners);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]), canvas.worldCamera, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]), canvas.worldCamera, out Vector2 max);

        highlight.anchorMin = new Vector2(0.5f, 0.5f);
        highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        highlight.anchoredPosition = (min + max) * 0.5f;
        highlight.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)) + Vector2.one * highlightPadding;
        LayoutHighlightLines();
    }

    void LayoutHighlightLines()
    {
        if (highlightLines == null || highlightLines.Length < 4) return;

        SetLine(highlightLines[0], new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, highlightThickness));
        SetLine(highlightLines[1], new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, highlightThickness));
        SetLine(highlightLines[2], new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(highlightThickness, 0f));
        SetLine(highlightLines[3], new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(highlightThickness, 0f));
    }

    static void SetLine(Image line, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        if (line == null) return;

        RectTransform rect = line.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
    }

    void OnCardPlayed(CardData card)
    {
        if (step == Step.PlayFirstChip)
            SetStep(Step.EndFirstTurn);
        else if (step == Step.CorruptionWarning)
            SetStep(Step.Complete);
    }

    void OnHandCorrupted(int slotIndex, CardData corruptedCard)
    {
        if (step == Step.Complete) return;

        SetStep(Step.CorruptionWarning);
        SetHighlight(GetChipAnchor(slotIndex));
    }

    RectTransform GetChipAnchor(int slotIndex)
    {
        if (chipAnchors == null || chipAnchors.Length == 0)
            ResolveChipAnchors();

        if (chipAnchors != null && slotIndex >= 0 && slotIndex < chipAnchors.Length && chipAnchors[slotIndex] != null)
            return chipAnchors[slotIndex];

        return firstChipAnchor;
    }

    void OnStateChanged(CombatState state)
    {
        if (state == CombatState.Win || state == CombatState.Lose)
        {
            if (step != Step.Complete) SetStep(Step.Complete);
            return;
        }

        if (step == Step.EndFirstTurn && state == CombatState.EnemyTurn)
        {
            sawEnemyTurn = true;
            SetStep(Step.EnemyTurn);
        }
        else if (step == Step.EnemyTurn && sawEnemyTurn && state == CombatState.PlayerTurn)
        {
            SetStep(Step.WaitForCorruption);
        }
    }
}
