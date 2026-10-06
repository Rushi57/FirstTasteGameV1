using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MechanicRule
{
    [Tooltip("The item id that, when dropped here, should trigger this rule (e.g. 'Knife', 'PanLid', 'Spatula', 'WaterBottle').")]
    public string triggerItemId;

    [Tooltip("Optional: another item id that must already be in this zone first (e.g. 'Onion' before 'Knife' can trigger cutting). Leave blank if there's no precondition.")]
    public string requiresPriorItemId;

    [Tooltip("Panel/GameObject to activate - matches your existing pattern of mechanics auto-starting via OnEnable.")]
    public GameObject mechanicPanel;

    [Tooltip("Optional: if set, also reports this id to the tutorial system as a completed action.")]
    public string tutorialReportId;
}

/// <summary>
/// A surface items get dropped onto (chopping board, pot, pan). Tracks which item
/// ids are currently present and fires the matching MechanicRule when the right
/// item is dropped - optionally requiring another item already be present first
/// (e.g. the knife only triggers cutting once an ingredient is already on the board;
/// the pan lid or spatula can trigger their mechanics on their own).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DropZone : MonoBehaviour
{
    [SerializeField] private string zoneId;
    [SerializeField] private List<MechanicRule> rules;

    private readonly HashSet<string> itemsPresent = new HashSet<string>();
    private RectTransform rt;

    public string ZoneId => zoneId;
    public RectTransform RectTransform => rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    public bool Contains(string itemId) => itemsPresent.Contains(itemId);

    public void NotifyItemDropped(string itemId)
    {
        itemsPresent.Add(itemId);

        foreach (var rule in rules)
        {
            if (rule.triggerItemId != itemId) continue;

            if (!string.IsNullOrEmpty(rule.requiresPriorItemId) &&
                !itemsPresent.Contains(rule.requiresPriorItemId))
                continue; // precondition item isn't in the zone yet - don't trigger

            Activate(rule);
        }
    }

    public void NotifyItemRemoved(string itemId)
    {
        itemsPresent.Remove(itemId);
    }

    void Activate(MechanicRule rule)
    {
        if (rule.mechanicPanel != null)
            rule.mechanicPanel.SetActive(true);

        if (!string.IsNullOrEmpty(rule.tutorialReportId) && TutorialManager.Instance != null)
            TutorialManager.Instance.ReportActionCompleted(rule.tutorialReportId);
    }
}