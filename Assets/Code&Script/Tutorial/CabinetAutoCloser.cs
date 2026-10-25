using System.Collections;
using UnityEngine;

public class CabinetAutoCloser : MonoBehaviour
{
    [SerializeField] private GameObject cabinetMain;

    [Tooltip("Step ids that should close the cabinet, e.g. the 'pan on stove' id")]
    [SerializeField] private string[] closeOnStepIds;
    [Tooltip("Ignore the ids above and close after ANY completed step")]
    [SerializeField] private bool closeOnAnyStep = false;

    [SerializeField] private float closeDelay = 0.3f;

    public static CabinetAutoCloser Instance { get; private set; }

    private Coroutine closeRoutine;

    private void Awake() => Instance = this;

    private void OnEnable() => CookingPrepListUI.StepCompleted += OnStepCompleted;
    private void OnDisable() => CookingPrepListUI.StepCompleted -= OnStepCompleted;

    private void OnStepCompleted(string stepId)
    {
        if (!closeOnAnyStep && System.Array.IndexOf(closeOnStepIds, stepId) < 0) return;
        Close();   // only one close, no extra coroutine here
    }

    public void Close()
    {
        if (cabinetMain == null || !cabinetMain.activeInHierarchy) return;
        if (closeRoutine != null) StopCoroutine(closeRoutine);
        closeRoutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        yield return new WaitForSeconds(closeDelay);   // let the item finish snapping/dropping
        cabinetMain.SetActive(false);                  // no button click, so no tutorial report
        closeRoutine = null;
    }
}