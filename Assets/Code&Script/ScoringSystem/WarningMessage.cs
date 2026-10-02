using System.Collections;
using UnityEngine;
using TMPro;

public class WarningMessageUI : MonoBehaviour
{
    public static WarningMessageUI Instance { get; private set; }

    [Tooltip("The WarningBoarder object (the white box). Hidden until needed.")]
    public GameObject board;
    public TMP_Text messageText;
    public float showSeconds = 1.5f;

    private Coroutine routine;

    private void Awake()
    {
        Instance = this;
        if (board != null) board.SetActive(false);
    }

    public void Show(string message)
    {
        if (messageText != null) messageText.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        board.SetActive(true);
        yield return new WaitForSeconds(showSeconds);
        board.SetActive(false);
        routine = null;
    }
}