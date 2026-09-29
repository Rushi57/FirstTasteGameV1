using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PanDropZone : MonoBehaviour, IDropHandler
{
    [Header("Requirements")]
    public bool requireHeat = true;

    [Header("On accept")]
    public bool consumeIngredient = false;

    [Header("Placement")]
    [Tooltip("Where accepted ingredients snap to. Leave empty to use this object's own RectTransform.")]
    public RectTransform ingredientAnchor;

    public event System.Action<IngredientData> OnIngredientAdded;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null || dropped == gameObject) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null) return;

        var interactable = dropped.GetComponent<TutorialInteractable>();
        IngredientData data = interactable != null ? interactable.sourceData as IngredientData : null;

        if (data == null)
        {
            Debug.LogWarning($"[PanDrop] {dropped.name} has no IngredientData (TutorialInteractable.sourceData) - ignoring.");
            return;
        }

        StoveHeatController stove = GetComponentInParent<StoveHeatController>();

        if (requireHeat && (stove == null || stove.CurrentHeat == StoveHeat.Off))
        {
            Debug.Log($"[PanDrop] '{data.id}' dropped but the stove isn't heated - rejecting.");
            ScoreManager.Instance?.ReportResult(ResultQuality.Bad);
            return;
        }

        string stepId = $"Pan:{data.id}";
        Debug.Log($"[PanDrop] Added '{data.id}' to the pan -> completing '{stepId}'");

        CookingPrepListUI.Instance?.CompleteStep(stepId);
        OnIngredientAdded?.Invoke(data);

        RectTransform target = ingredientAnchor != null ? ingredientAnchor : transform as RectTransform;

        if (consumeIngredient)
        {
            dropped.transform.SetParent(target, false);
            dropped.SetActive(false);
        }
        else
        {
            drag.SnapTo(target);
        }
    }
}