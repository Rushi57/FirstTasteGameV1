using System.Collections;
using TMPro;
using UnityEngine;

public class SaveSlotMessage : MonoBehaviour
{
    [SerializeField] private GameObject messagePanel;  
    [SerializeField] private TMP_Text messageText;     
    [SerializeField] private float showSeconds = 2.5f;

    private void Start()
    {
        int slot = GameSession.PendingSavedSlotMessage;
        if (slot < 0) return;

        GameSession.PendingSavedSlotMessage = -1;
        messageText.text = $"Your progress is saved in slot {slot + 1}";
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        messagePanel.SetActive(true);
        yield return new WaitForSecondsRealtime(showSeconds);
        messagePanel.SetActive(false);
    }
}