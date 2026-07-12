using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First-pass Area 1 tutorial: a non-blocking card overlay that advances from real combat events.
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

    [Header("Scene references")]
    [SerializeField] private CombatManager combat;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform firstChipAnchor;
    [SerializeField] private RectTransform endTurnAnchor;
    [SerializeField] private InGameSettingsPanelController settingsPanel;
    [SerializeField] private NeuralInterfaceHUD neuralHud;

    [Header("Tuning")]
    [SerializeField] private Vector2 cardSize = new Vector2(430f, 235f);
    [SerializeField] private Vector2 cardOffset = new Vector2(34f, -34f);
    [SerializeField] private float highlightPadding = 18f;
    [SerializeField] private float highlightThickness = 7f;
    [SerializeField] private float highlightPulseScale = 1.08f;
    [SerializeField] private float finalCardSeconds = 5f;

    RectTransform overlay;
    RectTransform card;
    RectTransform highlight;
    Image[] highlightLines;
    RectTransform[] chipAnchors;
    TMP_Text titleText;
    TMP_Text bodyText;
    TMP_Text objectiveText;
    CanvasGroup cardGroup;
    RectTransform currentHighlightTarget;
    Step step;
    bool sawEnemyTurn;
    float completeTimer;

    void OnEnable()
    {
        ResolveReferences();
        BuildUi();

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

        if (overlay != null) Destroy(overlay.gameObject);
        overlay = null;
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
                Destroy(overlay.gameObject);
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

    void BuildUi()
    {
        if (canvas == null || overlay != null) return;

        overlay = CreateRect("Area1_TutorialOverlay", canvas.transform);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();

        highlight = CreateRect("TutorialHighlight", overlay);
        highlightLines = new[]
        {
            CreateHighlightLine("Top", highlight),
            CreateHighlightLine("Bottom", highlight),
            CreateHighlightLine("Left", highlight),
            CreateHighlightLine("Right", highlight)
        };

        card = CreateRect("TutorialCard", overlay);
        card.anchorMin = new Vector2(0f, 1f);
        card.anchorMax = new Vector2(0f, 1f);
        card.pivot = new Vector2(0f, 1f);
        card.anchoredPosition = cardOffset;
        card.sizeDelta = cardSize;
        cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        cardGroup.blocksRaycasts = false;

        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.raycastTarget = false;
        cardImage.color = new Color(0.015f, 0.025f, 0.045f, 0.92f);
        Outline cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.1f, 0.92f, 1f, 0.7f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        titleText = CreateText("Title", card, 24, FontStyles.UpperCase);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        titleText.rectTransform.sizeDelta = new Vector2(-42f, 34f);
        titleText.color = new Color(0.66f, 1f, 1f, 1f);

        bodyText = CreateText("Body", card, 18, FontStyles.Normal);
        bodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
        bodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        bodyText.rectTransform.offsetMin = new Vector2(22f, 76f);
        bodyText.rectTransform.offsetMax = new Vector2(-22f, -62f);
        bodyText.color = new Color(0.9f, 0.96f, 1f, 1f);

        objectiveText = CreateText("Objective", card, 16, FontStyles.Bold);
        objectiveText.rectTransform.anchorMin = new Vector2(0f, 0f);
        objectiveText.rectTransform.anchorMax = new Vector2(1f, 0f);
        objectiveText.rectTransform.pivot = new Vector2(0.5f, 0f);
        objectiveText.rectTransform.anchoredPosition = new Vector2(0f, 18f);
        objectiveText.rectTransform.sizeDelta = new Vector2(-42f, 48f);
        objectiveText.color = new Color(1f, 0.87f, 0.42f, 1f);
    }

    static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    static TMP_Text CreateText(string objectName, Transform parent, int size, FontStyles style)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.fontSize = size;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    static Image CreateHighlightLine(string objectName, Transform parent)
    {
        RectTransform rect = CreateRect(objectName, parent);

        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(0.35f, 1f, 1f, 0.95f);
        return image;
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
                SetCard("Memory Chips",
                    "Each card is a chip in the neural rack. Click one to install it, spend CPU cycles, and make Vestige act.",
                    "Objective: play any chip.");
                SetHighlight(firstChipAnchor);
                break;
            case Step.EndFirstTurn:
                SetCard("End The Turn",
                    "CPU pips are your action budget. When you are done installing chips, pass control and let the enemy move.",
                    "Objective: press End Turn when ready.");
                SetHighlight(endTurnAnchor);
                break;
            case Step.EnemyTurn:
                SetCard("Enemy Thread",
                    "Enemies act after your turn. Block protects this turn only, while shields can stop later hits.",
                    "Objective: survive the enemy turn.");
                SetHighlight(null);
                break;
            case Step.WaitForCorruption:
                SetCard("Keep Cutting",
                    "The Loom watches your pattern. On every third player turn, it copies one chip into corrupted memory.",
                    "Objective: play and end turns until the copy appears.");
                SetHighlight(null);
                break;
            case Step.CorruptionWarning:
                SetCard("The Loom Copies You",
                    "Every few turns, the Loom corrupts one of your chips. Corrupted chips cannot be installed and too many will overload memory.",
                    "Objective: watch the corrupted chip.");
                break;
            case Step.Complete:
                SetCard("Tutorial Complete",
                    "That is the loop: install chips, spend CPU wisely, end the turn, and watch the Loom's corrupted copies.",
                    "Area 1 training uploaded.");
                SetHighlight(null);
                break;
        }
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
