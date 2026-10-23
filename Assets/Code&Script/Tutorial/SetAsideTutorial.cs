using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the "use the bowl to set aside..." message ONCE, only when the
/// Cooking Prep panel opens for the target recipe (Chicken Asado).
/// The "already seen" flag is saved in PlayerPrefs, so it never shows again,
/// even after finishing the game and replaying.
///
/// IMPORTANT: put this script on an object that is ALWAYS ACTIVE
/// (e.g. the same object as CookingPrepPanelController),
/// NOT on the SetAsideInstruction object itself (it starts inactive).
/// </summary>
public class SetAsideTutorial : MonoBehaviour
{
    [Header("Target dish")]
    [Tooltip("Drag the Chicken Asado RecipeData asset here.")]
    public RecipeData targetRecipe;

    [Header("UI")]
    [Tooltip("The SetAsideInstruction GameObject.")]
    public GameObject instructionObject;
    [Tooltip("The Continue button inside SetAsideInstruction.")]
    public Button continueButton;

    [Header("Behaviour")]
    [Tooltip("0 = wait for Continue. >0 = also auto-hide after this many seconds.")]
    public float autoHideSeconds = 0f;

    [Tooltip("Unique key so each tutorial has its own saved flag.")]
    public string saveKey = "Tutorial_SetAsideBowl_ChickenAsado";

    private Coroutine autoHideRoutine;

    private void Awake()
    {
        if (instructionObject != null) instructionObject.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Hide);
    }

    /// <summary>Call this when the Cooking Prep panel is shown.</summary>
    public void TryShow(RecipeData currentRecipe)
    {
        if (currentRecipe == null || targetRecipe == null) return;
        if (currentRecipe != targetRecipe) return;   // not Chicken Asado
        if (HasSeen()) return;                       // already shown once

        MarkSeen();                                  // save immediately so it never repeats
        instructionObject.SetActive(true);

        if (autoHideSeconds > 0f)
            autoHideRoutine = StartCoroutine(AutoHide());
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        Hide();
    }

    public void Hide()
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }
        if (instructionObject != null) instructionObject.SetActive(false);
    }

    private bool HasSeen() => PlayerPrefs.GetInt(saveKey, 0) == 1;

    private void MarkSeen()
    {
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>For testing: right-click the component header > Reset Tutorial.</summary>
    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log($"[SetAsideTutorial] Reset '{saveKey}'");
    }
}