using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiSelectionHelper
{
    public static void SelectFirst(GameObject root, GameObject preferred = null)
    {
        if (EventSystem.current == null) return;

        GameObject target = IsSelectable(preferred) ? preferred : FindFirstSelectable(root);
        EventSystem.current.SetSelectedGameObject(target);
    }

    public static void RestoreIfMissing(GameObject root, GameObject preferred = null)
    {
        if (EventSystem.current == null) return;
        if (IsSelectable(EventSystem.current.currentSelectedGameObject)) return;

        SelectFirst(root, preferred);
    }

    public static GameObject FindFirstSelectable(GameObject root)
    {
        if (root == null) return null;

        Selectable[] controls = root.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable control in controls)
            if (control != null && control.IsActive() && control.IsInteractable())
                return control.gameObject;

        return null;
    }

    private static bool IsSelectable(GameObject go)
    {
        if (go == null) return false;

        Selectable selectable = go.GetComponent<Selectable>();
        return selectable != null && selectable.IsActive() && selectable.IsInteractable();
    }
}
