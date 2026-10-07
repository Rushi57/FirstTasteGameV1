using System.Collections;
using UnityEngine;
using TMPro;

public class WarningMessageUI : MonoBehaviour
{
    public static WarningMessageUI Instance { get; private set; }

    public GameObject board;
    public TMP_Text messageText;
    public float showSeconds = 1.5f;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (board != null) board.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;   // don't leave a dead reference behind
    }

    public void Show(string message)
    {
        if (!isActiveAndEnabled || board == null) return;   // can't run a coroutine if inactive

        if (messageText != null) messageText.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        board.SetActive(true);
        yield return new WaitForSecondsRealtime(showSeconds);
        board.SetActive(false);
        routine = null;
    }
}