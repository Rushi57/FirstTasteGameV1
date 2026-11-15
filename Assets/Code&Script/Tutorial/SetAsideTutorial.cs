using System.Collections;
using UnityEngine;
using UnityEngine.UI;


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

    public void TryShow(RecipeData currentRecipe)
    {
        if (currentRecipe == null || targetRecipe == null) return;
        if (currentRecipe != targetRecipe) return;   
        if (HasSeen()) return;                     

        MarkSeen();                                 
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


    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log($"[SetAsideTutorial] Reset '{saveKey}'");
    }
}