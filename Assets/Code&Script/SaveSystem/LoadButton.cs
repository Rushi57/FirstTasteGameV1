using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class LoadGameButton : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("Load Panel")]
    [SerializeField] private GameObject noSavePanel;
    [SerializeField] private Animator noSaveAnimator;
    [SerializeField] private GameObject noSaveText;
    [SerializeField] private GameObject slotList;
    [SerializeField] private string openTrigger = "MainMenuAnimOpen";
    [SerializeField] private string closeTrigger = "MainMenuAnimClose";

    private SaveSlotButton[] slots;

    private void Awake()
    {
        slots = slotList.GetComponentsInChildren<SaveSlotButton>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(i, OnSlotClicked);
    }

    public void OnLoadClicked()
    {
        bool anySave = SaveSystem.AnySaveExists();

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
        if (data == null) return;

        GameSession.Data = data;
        GameSession.CurrentSlot = slot;
        TutorialManager.ApplySavedTutorialState(data);
        LoadingManager.LoadNextScene(mapSceneName);
    }

    public void OnCloseClicked()
    {
        if (noSaveAnimator != null)
        {
            noSaveAnimator.ResetTrigger(openTrigger);
            noSaveAnimator.SetTrigger(closeTrigger);
        }
    }
}