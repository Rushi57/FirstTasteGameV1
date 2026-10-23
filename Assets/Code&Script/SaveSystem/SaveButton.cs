using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach to SaveBtn in MapScene (SaveBtn must stay ACTIVE; the panel below starts inactive).
[RequireComponent(typeof(Button))]
public class SaveButton : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject savePanel;            // SaveMessage
    [SerializeField] private GameObject slotList;             // ScrollSave
    [SerializeField] private GameObject saveCompleteMessage;  // MessageForSaveComplete
    [SerializeField] private GameObject confirmationPanel;    // MessageConfirmation
    [Header("Buttons")]
    [SerializeField] private Button confirmButton;            // Confirm (inside MessageConfirmation)
    [SerializeField] private Button cancelButton;             // Cancel (inside MessageConfirmation)
    [SerializeField] private Button closeButton;              // CloseButton
    [Header("Options")]
    [SerializeField] private float completeMessageSeconds = 1f;
    [SerializeField] private bool closePanelAfterSave = false;

    private SaveSlotButton[] slots;
    private int pendingSlot = -1;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OpenPanel);

        slots = slotList.GetComponentsInChildren<SaveSlotButton>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(i, OnSlotClicked);

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    public void OpenPanel()
    {
        saveCompleteMessage.SetActive(false);
        confirmationPanel.SetActive(false);
        foreach (var s in slots) s.Refresh();
        savePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        confirmationPanel.SetActive(false);
        saveCompleteMessage.SetActive(false);
        savePanel.SetActive(false);
    }

    private void OnSlotClicked(int slot)
    {
        pendingSlot = slot;

        if (SaveSystem.HasSave(slot))
            confirmationPanel.SetActive(true);   // "Replace Save Data" Cancel / Confirm
        else
            DoSave(slot);                        // empty slot -> save right away
    }

    private void OnConfirm()
    {
        confirmationPanel.SetActive(false);
        if (pendingSlot >= 0) DoSave(pendingSlot);
    }

    private void DoSave(int slot)
    {
        SaveData data = GameSession.GetOrCreateData();

        if (!data.unlockedLevels.Contains(1)) data.unlockedLevels.Add(1);
        TutorialManager.WriteCompletedTutorialsTo(data);
        data.tutorialDone = data.IsTutorialCompleted("main_tutorial_MAP");

        SaveSystem.Save(data, slot);
        GameSession.CurrentSlot = slot;

        slots[slot].Refresh();
        StopAllCoroutines();
        StartCoroutine(ShowCompleteRoutine());
    }

    private IEnumerator ShowCompleteRoutine()
    {
        saveCompleteMessage.SetActive(true);
        yield return new WaitForSecondsRealtime(completeMessageSeconds);
        saveCompleteMessage.SetActive(false);
        if (closePanelAfterSave) savePanel.SetActive(false);
    }
}