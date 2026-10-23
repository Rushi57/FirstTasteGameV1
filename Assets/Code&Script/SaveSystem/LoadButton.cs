using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to LoadGameBtn in MainMenu.
[RequireComponent(typeof(Button))]
public class LoadGameButton : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("Load Panel")]
    [SerializeField] private GameObject noSavePanel;      // LoadGameMainMenuPanel
    [SerializeField] private Animator noSaveAnimator;     // Animator on LoadMainMenuBorder
    [SerializeField] private GameObject noSaveText;       // Text (TMP) "No Save Data!"
    [SerializeField] private GameObject slotList;         // ScrollSave
    [SerializeField] private string openTrigger = "MainMenuAnimOpen";
    [SerializeField] private string closeTrigger = "MainMenuAnimClose";

    private SaveSlotButton[] slots;

    private void Awake()
    {
        slots = slotList.GetComponentsInChildren<SaveSlotButton>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(i, OnSlotClicked);
    }

    // Wire to LoadGameBtn's OnClick()
    public void OnLoadClicked()
    {
        bool anySave = SaveSystem.AnySaveExists();

        // No saves at all -> only the "No Save Data!" text. Otherwise -> the slot list.
        noSaveText.SetActive(!anySave);
        slotList.SetActive(anySave);
        if (anySave) foreach (var s in slots) s.Refresh();

        if (!noSavePanel.activeSelf) noSavePanel.SetActive(true);
        if (noSaveAnimator != null)
        {
            noSaveAnimator.ResetTrigger(closeTrigger);
            noSaveAnimator.SetTrigger(openTrigger);
        }
    }

    private void OnSlotClicked(int slot)
    {
        SaveData data = SaveSystem.Load(slot);
        if (data == null) return; // empty slot: nothing to load

        GameSession.Data = data;
        GameSession.CurrentSlot = slot;
        TutorialManager.ApplySavedTutorialState(data);
        LoadingManager.LoadNextScene(mapSceneName);
    }

    // Wire to CloseBtn's OnClick()
    public void OnCloseClicked()
    {
        if (noSaveAnimator != null)
        {
            noSaveAnimator.ResetTrigger(openTrigger);
            noSaveAnimator.SetTrigger(closeTrigger);
        }
    }
}