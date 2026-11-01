using System;
using UnityEngine;

public class PanelExclusivityManager : MonoBehaviour
{
    public static PanelExclusivityManager Instance { get; private set; }

    [Serializable]
    public class PanelEntry
    {
        public GameObject panel;
        public string displayName;   // e.g. "Fridge", "Cabinet", "Shelf"
    }

    [Header("Shared warning UI (same one CookingStepGate uses)")]
    [SerializeField] private WarningMessageUI warning;

    [Header("Panel names used in the warning")]
    [SerializeField] private PanelEntry[] panelNames;

    [Tooltip("{0} is replaced with the open panel's name")]
    [SerializeField] private string alreadyOpenMessage = "Close the {0} first!";
    [Tooltip("Used when the open panel has no entry in the list above")]
    [SerializeField] private string fallbackMessage = "Close the open panel first!";

    private GameObject currentOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// Returns true if the panel may open.
    public bool TryOpen(GameObject panel)
    {
        if (currentOpen != null && !currentOpen.activeInHierarchy)
            currentOpen = null;

        // Catch panels that were opened without TryOpen
        if (currentOpen == null && panelNames != null)
        {
            foreach (var entry in panelNames)
            {
                if (entry.panel != null && entry.panel != panel && entry.panel.activeInHierarchy)
                {
                    currentOpen = entry.panel;
                    break;
                }
            }
        }

        if (currentOpen != null && currentOpen != panel)
        {
            warning?.Show(BuildMessage(currentOpen));
            return false;
        }

        currentOpen = panel;
        panel.SetActive(true);
        return true;
    }

    public void Close(GameObject panel)
    {
        if (currentOpen == panel) currentOpen = null;
        panel.SetActive(false);
    }

    private string BuildMessage(GameObject openPanel)
    {
        string name = GetDisplayName(openPanel);
        return string.IsNullOrEmpty(name)
            ? fallbackMessage
            : string.Format(alreadyOpenMessage, name);
    }

    private string GetDisplayName(GameObject panel)
    {
        if (panelNames == null) return null;
        foreach (var entry in panelNames)
            if (entry.panel == panel && !string.IsNullOrEmpty(entry.displayName))
                return entry.displayName;
        return null;
    }
}