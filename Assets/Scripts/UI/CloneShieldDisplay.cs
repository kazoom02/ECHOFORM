using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// ECHOFORM — CloneShieldDisplay
// Apresenta na interface a quantidade de escudos do clone e atualiza
// o ícone e o valor sempre que o estado do inimigo se altera.
// =====================================================

public class CloneShieldDisplay : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private PlayerClone clone;
    [SerializeField] private bool autoBind = true;

    [Header("UI")]
    [SerializeField] private Image shieldAmount;
    [SerializeField] private TMP_Text shieldAmountText;
    [SerializeField] private bool showSingleShieldAmount = false;

    [Header("Sprites")]
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private Sprite deactivatedSprite;

    void Awake()
    {
        if (autoBind && clone == null)
            clone = GetComponentInParent<PlayerClone>();

        if (shieldAmount == null)
            shieldAmount = FindNamedComponent<Image>("ShieldAmount");

        if (shieldAmountText == null)
            shieldAmountText = FindNamedComponent<TMP_Text>("ShieldAmountText");
    }

    void OnEnable()
    {
        if (clone != null) clone.OnStateChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (clone != null) clone.OnStateChanged -= Refresh;
    }

    void Refresh()
    {
        int shields = clone != null ? clone.Shields : 0;
        bool hasShield = shields > 0;

        if (shieldAmount != null)
        {
            Sprite sprite = hasShield ? activatedSprite : deactivatedSprite;
            if (sprite != null) shieldAmount.sprite = sprite;
            shieldAmount.enabled = sprite != null || shieldAmount.sprite != null;
        }

        if (shieldAmountText != null)
        {
            bool showAmount = shields > 1 || (showSingleShieldAmount && shields > 0);
            shieldAmountText.gameObject.SetActive(showAmount);
            shieldAmountText.text = showAmount ? shields.ToString() : string.Empty;
        }
    }

    T FindNamedComponent<T>(string objectName) where T : Component
    {
        Transform root = transform.root;
        T found = FindNamedComponentInChildren<T>(transform, objectName);
        if (found != null) return found;

        if (clone != null)
        {
            found = FindNamedComponentInChildren<T>(clone.transform, objectName);
            if (found != null) return found;
        }

        found = root != null ? FindNamedComponentInChildren<T>(root, objectName) : null;
        if (found != null) return found;

        foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (component.name == objectName) return component;

        return null;
    }

    static T FindNamedComponentInChildren<T>(Transform root, string objectName) where T : Component
    {
        if (root == null) return null;

        foreach (T component in root.GetComponentsInChildren<T>(true))
            if (component.name == objectName) return component;

        return null;
    }
}
