using UnityEngine;

public class PanelExclusivityManager : MonoBehaviour
{
    public static PanelExclusivityManager Instance { get; private set; }

    [Header("Shared warning UI (same one CookingStepGate uses)")]
    [SerializeField] private WarningMessageUI warning;
    [SerializeField] private string alreadyOpenMessage = "Close the open panel first!";

    private GameObject currentOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// Returns true if the panel may open.
    public bool TryOpen(GameObject panel)
    {
        if (currentOpen != null && currentOpen != panel)
        {
            warning?.Show(alreadyOpenMessage);   // message only, no ScoreManager call
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
}