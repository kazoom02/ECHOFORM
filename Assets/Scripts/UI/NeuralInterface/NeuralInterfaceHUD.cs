using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================
// ECHOFORM — NeuralInterfaceHUD
// Sincroniza a mão e a energia com a interface neural e coordena a seleção,
// instalação, animação e execução dos efeitos das cartas.
// =====================================================

[System.Serializable] public class CardEvent : UnityEvent<CardData> { }

public class NeuralInterfaceHUD : MonoBehaviour
{
    public enum ControllerFamily { None, Xbox, PlayStation, Nintendo, Generic }

    [Header("Combat")]
    [SerializeField] private CombatManager combat;
    [SerializeField] private int maxCycles = 5;

    [Header("Rack")]
    [Tooltip("Empty RectTransforms placed over each bay, left to right.")]
    [SerializeField] private RectTransform[] rackSlots;
    [SerializeField] private ChipView chipPrefab;

    [Header("Neural slot")]
    [SerializeField] private RectTransform neuralSlot;
    [SerializeField] private Vector3 installedScale = Vector3.one;
    [SerializeField] private CanvasGroup installGroup;
    [SerializeField] private TMP_Text installLabel;
    [SerializeField] private Image installBar;

    [Header("FX")]
    [SerializeField] private VisorFlash visorFlash;
    [SerializeField] private ScreenSlash screenSlash;
    [SerializeField] private ChargedSlashFX chargedSlashPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [Tooltip("Only this card (by cardName) triggers the full-screen slash. Spaces/case are ignored.")]
    [SerializeField] private string slashCardName = "Charged";
    [SerializeField] private CpuCycleMeter cpuMeter;
    [SerializeField] private OverloadReadout overloadReadout;
    [SerializeField] private CombatTargeting targeting;
    [SerializeField] private VestigeCombatAnimator vestige;

    [Header("Timing (seconds)")]
    [SerializeField] private float ejectHeight = 60f;
    [SerializeField] private float ejectTime   = 0.12f;
    [SerializeField] private float slideTime   = 0.25f;
    [SerializeField] private float installTime = 0.80f;
    [SerializeField] private float settleTime  = 0.15f;

    [Header("Controller")]
    [SerializeField] private bool controllerChipSelection = true;
    [SerializeField] private float navigationRepeatDelay = 0.18f;

    [Header("Events")]
    [Tooltip("Fires the moment the memory is installed — hook Vestige's dash+attack, SFX, screen shake.")]
    public CardEvent onChipInstalled;

    readonly List<ChipView> rack = new List<ChipView>();
    bool busy;
    ChipView pendingTargetView;
    int selectedChipIndex = -1;
    float nextNavigationTime;
    bool showControllerSelection;
    bool transitionSuppressed;
    bool externalInputBlocked;

    public bool IsBusy => busy;
    public bool IsControllerSelectionVisible => showControllerSelection;
    public ControllerFamily CurrentController => DetectControllerFamily();
    public string ConfirmButtonName => GetConfirmButtonName(CurrentController);
    public System.Action<bool> OnBusyChanged;

    void OnEnable()  { if (combat != null) combat.OnCombatChanged += Refresh; }
    void OnDisable()
    {
        if (combat != null) combat.OnCombatChanged -= Refresh;
        ClearControllerSelection();
        SetBusy(false);
    }
    void Start()     { if (installGroup) installGroup.alpha = 0f; Refresh(); }

    void Update()
    {
        if (AreaTransition.IsPlaying)
        {
            if (!transitionSuppressed)
            {
                transitionSuppressed = true;
                ClearControllerSelection();
            }
            return;
        }
        transitionSuppressed = false;

        if (externalInputBlocked || !controllerChipSelection || busy || rack.Count == 0 || Time.timeScale == 0f) return;
        if (targeting != null && targeting.IsTargeting) return;

        int direction = ReadNavigationDirection();
        if (direction != 0)
        {
            showControllerSelection = true;
            SelectChip(selectedChipIndex + direction);
        }

        if (ConfirmPressed() && selectedChipIndex >= 0 && selectedChipIndex < rack.Count)
        {
            showControllerSelection = true;
            OnChipClicked(rack[selectedChipIndex]);
        }
    }

    public void Refresh()
    {
        foreach (var v in rack) if (v) Destroy(v.gameObject);
        rack.Clear();

        var hand = (combat != null && combat.Deck != null) ? combat.Deck.Hand : null;
        if (hand != null)
            for (int i = 0; i < rackSlots.Length && i < hand.Count; i++)
                Spawn(hand[i], rackSlots[i]);

        if (rack.Count == 0)
        {
            selectedChipIndex = -1;
            showControllerSelection = false;
        }
        else SelectChip(Mathf.Clamp(selectedChipIndex < 0 ? 0 : selectedChipIndex, 0, rack.Count - 1));

        if (cpuMeter != null)
            cpuMeter.Set(
                combat != null ? combat.Energy : maxCycles,
                combat != null ? combat.MaxEnergy : maxCycles);

        if (overloadReadout != null)
            overloadReadout.Set(combat != null ? combat.CorruptedInHand : 0);
    }

    void Spawn(CardData card, RectTransform slot)
    {
        ChipView v = Instantiate(chipPrefab, slot);
        v.Rect.anchoredPosition = Vector2.zero;
        v.Rect.localScale = Vector3.one;
        v.Bind(card);
        v.Clicked += OnChipClicked;
        v.Hovered += OnChipHovered;
        rack.Add(v);
    }

    void OnChipHovered(ChipView view)
    {
        int chipIndex = rack.IndexOf(view);
        if (chipIndex < 0) return;

        showControllerSelection = false;
        SelectChip(chipIndex);
    }

    void OnChipClicked(ChipView view)
    {
        if (view == null || view.card == null) return;
        int chipIndex = rack.IndexOf(view);
        if (chipIndex >= 0) SelectChip(chipIndex);

        if (targeting != null && targeting.IsTargeting)
        {
            ChipView previous = pendingTargetView;
            targeting.Cancel();
            if (previous == view) return;
        }

        if (busy) return;

        bool cannotPlay = combat != null ? !combat.CanPlayCard(view.card) : view.card.isGlitch;
        if (cannotPlay) { StartCoroutine(Deny(view)); return; }

        CardTarget effectiveTarget = combat != null
            ? combat.GetEffectiveTarget(view.card)
            : view.card.target;
        if (targeting != null && effectiveTarget == CardTarget.SingleEnemy && CountLivingEnemies() > 1)
        {
            SetBusy(true);
            pendingTargetView = view;
            targeting.Begin(
                picked => { pendingTargetView = null; StartCoroutine(InstallRoutine(view, picked)); },
                ()     => { pendingTargetView = null; SetBusy(false); }
            );
            return;
        }

        StartCoroutine(InstallRoutine(view, ResolveTarget(view.card)));
    }

    int CountLivingEnemies()
    {
        if (combat == null) return 0;
        int c = 0;
        foreach (var e in combat.Enemies) if (e != null && !e.IsDead) c++;
        return c;
    }

    IEnumerator InstallRoutine(ChipView view, Enemy target)
    {
        SetBusy(true);
        CardData card = view.card;
        CardData effectCard = combat != null ? combat.GetEffectSource(card) : card;
        rack.Remove(view);

        RectTransform layer = (RectTransform)neuralSlot.parent;
        RectTransform rt = view.Rect;
        rt.SetParent(layer, worldPositionStays: true);
        rt.SetAsLastSibling();

        Vector2 from = rt.anchoredPosition;
        Vector2 up   = from + Vector2.up * ejectHeight;
        yield return Move(rt, from, up, ejectTime, EaseOut);

        Vector2 slotPos = neuralSlot.anchoredPosition;
        yield return Move(rt, up, slotPos, slideTime, EaseInOut, rt.localScale, installedScale);

        if (installGroup) installGroup.alpha = 1f;
        float t = 0f, dotT = 0f; int dots = 0;
        while (t < installTime)
        {
            t += Time.deltaTime;
            if (installBar) installBar.fillAmount = Mathf.Clamp01(t / installTime);
            dotT += Time.deltaTime;
            if (dotT >= 0.2f) { dotT = 0f; dots = (dots + 1) % 4; if (installLabel) installLabel.text = "INSTALLING MEMORY" + new string('.', dots); }
            yield return null;
        }
        if (installBar) installBar.fillAmount = 1f;

        if (installGroup) installGroup.alpha = 0f;
        if (view) Destroy(view.gameObject);
        onChipInstalled?.Invoke(card);

        ChargedSlashFX slashFx = null;
        if (effectCard != null && IsSlashCard(effectCard))
        {
            if (chargedSlashPrefab != null)
                slashFx = ChargedSlashFX.Play(chargedSlashPrefab,
                    slashSpawnPoint != null ? slashSpawnPoint.position : Vector3.zero);
            else if (screenSlash != null)
                screenSlash.Slash();
        }
        bool dealsDamage = combat != null ? combat.WillDealDamage(card) : CardDealsDamage(effectCard);
        if (vestige != null && target != null && dealsDamage)
        {
            yield return vestige.PlayAttack(target.transform,
                () => { if (combat != null) combat.TryPlayCard(card, target); });
        }
        else
        {

            if (visorFlash && !IsSlashCard(effectCard))
                yield return visorFlash.FlashAndWait();
            if (combat != null) combat.TryPlayCard(card, target);
        }

        if (slashFx != null)
            yield return slashFx.WaitUntilFinished();

        SetBusy(false);
    }

    void SetBusy(bool value)
    {
        if (busy == value) return;
        busy = value;
        OnBusyChanged?.Invoke(busy);
    }

    void SelectChip(int index)
    {
        if (rack.Count == 0)
        {
            selectedChipIndex = -1;
            return;
        }

        selectedChipIndex = Mod(index, rack.Count);
        for (int i = 0; i < rack.Count; i++)
            if (rack[i] != null) rack[i].SetControllerSelected(showControllerSelection && i == selectedChipIndex);

        if (showControllerSelection && selectedChipIndex >= 0 && selectedChipIndex < rack.Count)
        {
            ChipView selected = rack[selectedChipIndex];
            if (selected != null && selected.card != null)
                CardTooltip.ShowAt(selected.card, TooltipScreenPosition(selected));
        }
    }

    public void ClearControllerSelection()
    {
        showControllerSelection = false;
        selectedChipIndex = -1;
        foreach (ChipView view in rack)
            if (view != null) view.SetControllerSelected(false);
        CardTooltip.Hide();
    }

    public void SetExternalInputBlocked(bool blocked)
    {
        externalInputBlocked = blocked;
        if (blocked) ClearControllerSelection();
    }

    Vector2 TooltipScreenPosition(ChipView view)
    {
        if (view == null) return Vector2.zero;

        RectTransform rect = view.Rect;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 topRight = corners[2];

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(cam, topRight);
    }

    static int Mod(int value, int count)
    {
        if (count <= 0) return 0;
        int result = value % count;
        return result < 0 ? result + count : result;
    }

    int ReadNavigationDirection()
    {
#if ENABLE_INPUT_SYSTEM
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
        nextNavigationTime = Time.unscaledTime + navigationRepeatDelay;
        return x < 0f ? -1 : 1;
#else
        if (Input.GetAxisRaw("Horizontal") < -0.5f && Time.unscaledTime >= nextNavigationTime)
        {
            nextNavigationTime = Time.unscaledTime + navigationRepeatDelay;
            return -1;
        }
        if (Input.GetAxisRaw("Horizontal") > 0.5f && Time.unscaledTime >= nextNavigationTime)
        {
            nextNavigationTime = Time.unscaledTime + navigationRepeatDelay;
            return 1;
        }
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.5f) nextNavigationTime = 0f;
        return 0;
#endif
    }

    static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;

        return false;
#else
        return Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
    }

    static ControllerFamily DetectControllerFamily()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad == null) return ControllerFamily.None;

        string description = $"{pad.layout} {pad.name} {pad.displayName} {pad.description.manufacturer} {pad.description.product} {pad.description.interfaceName}".ToLowerInvariant();
        if (description.Contains("dualsense") || description.Contains("dualshock") || description.Contains("playstation") || description.Contains("sony"))
            return ControllerFamily.PlayStation;
        if (description.Contains("xbox") || description.Contains("xinput") || description.Contains("microsoft"))
            return ControllerFamily.Xbox;
        if (description.Contains("switch") || description.Contains("nintendo"))
            return ControllerFamily.Nintendo;
        return ControllerFamily.Generic;
#else
        return ControllerFamily.Generic;
#endif
    }

    static string GetConfirmButtonName(ControllerFamily family)
    {
        switch (family)
        {
            case ControllerFamily.Xbox: return "A";
            case ControllerFamily.PlayStation: return "X";
            case ControllerFamily.Nintendo: return "B";
            case ControllerFamily.Generic: return "South";
            default: return "Enter";
        }
    }

    static bool CardDealsDamage(CardData c)
    {
        if (c == null) return false;
        foreach (var e in c.effects) if (e.type == CardEffectType.DealDamage) return true;
        return false;
    }

    bool IsSlashCard(CardData c)
    {
        if (c == null || string.IsNullOrEmpty(slashCardName)) return false;
        string a = (c.cardName ?? "").Replace(" ", "").ToLowerInvariant();
        string b = slashCardName.Replace(" ", "").ToLowerInvariant();
        return a == b && a.Length > 0;
    }

        protected virtual Enemy ResolveTarget(CardData card)
    {
        if (combat == null || combat.GetEffectiveTarget(card) != CardTarget.SingleEnemy) return null;
        foreach (var e in combat.Enemies) if (e != null && !e.IsDead) return e;
        return null;
    }

    IEnumerator Deny(ChipView view)
    {
        RectTransform rt = view.Rect;
        Vector2 home = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; rt.anchoredPosition = home + Vector2.right * Mathf.Sin(t * 60f) * 8f; yield return null; }
        rt.anchoredPosition = home;
    }

    delegate float Ease(float x);
    static float EaseOut(float x)   => 1f - (1f - x) * (1f - x);
    static float EaseInOut(float x) => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;

    IEnumerator Move(RectTransform rt, Vector2 a, Vector2 b, float dur, Ease ease, Vector3? sa = null, Vector3? sb = null)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = ease(Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.LerpUnclamped(a, b, p);
            if (sa.HasValue && sb.HasValue) rt.localScale = Vector3.LerpUnclamped(sa.Value, sb.Value, p);
            yield return null;
        }
        rt.anchoredPosition = b;
        if (sb.HasValue) rt.localScale = sb.Value;
    }
}
