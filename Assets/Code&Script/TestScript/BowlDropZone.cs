using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BowlDropZone : MonoBehaviour, IDropHandler
{
    [Header("Snap Settings")]
    [SerializeField] private float sideOffset = 60f;   
    [SerializeField] private float yOffset = 0f;      
    [SerializeField] private int maxIngredients = 2;

    private readonly List<RectTransform> ingredients = new List<RectTransform>();

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        RectTransform item = dropped.GetComponent<RectTransform>();
        if (item == null) return;

        TryAdd(item);
    }

    public bool TryAdd(RectTransform item)
    {
        if (ingredients.Contains(item)) { Relayout(); return true; }
        if (ingredients.Count >= maxIngredients) return false;

        ingredients.Add(item);
        item.SetParent(transform, false);
        Relayout();
        return true;
    }
    private void OnTransformChildrenChanged()
    {
        ingredients.RemoveAll(i => i == null || i.parent != transform);
        Relayout();
    }

    private void Relayout()
    {
        if (ingredients.Count == 1)
        {
           
            ingredients[0].anchoredPosition = new Vector2(0f, yOffset);
        }
        else if (ingredients.Count == 2)
        {
            ingredients[0].anchoredPosition = new Vector2(sideOffset, yOffset);
            ingredients[1].anchoredPosition = new Vector2(-sideOffset, yOffset);
        }
    }

    public int Count => ingredients.Count;
}