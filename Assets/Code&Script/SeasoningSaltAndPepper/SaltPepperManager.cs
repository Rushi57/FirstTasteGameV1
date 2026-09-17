using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SaltPepperManager : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    public Slider timerSlider;
    public TextMeshProUGUI currentAmountText;
    public TextMeshProUGUI targetAmountText;

    [Header("Game Settings")]
    public float timeLimit = 10f;
    public float targetAmount = 5f; // Target measurements needed to win

    private float currentAmount = 0f;
    private float timeRemaining;
    private bool isGameActive = false;

    private void OnEnable()
    {
        StartGame();
    }

    public void StartGame()
    {
        currentAmount = 0f;
        timeRemaining = timeLimit;
        isGameActive = true;

        if (timerSlider != null)
        {
            timerSlider.maxValue = timeLimit;
            timerSlider.value = timeLimit;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (!isGameActive) return;

        // Countdown Timer Slider
        timeRemaining -= Time.deltaTime;
        if (timerSlider != null)
        {
            timerSlider.value = timeRemaining;
        }

        if (timeRemaining <= 0f)
        {
            TimeOut();
        }
    }

    // Handles the drag-and-drop detection
    public void OnDrop(PointerEventData eventData)
    {
        if (!isGameActive) return;

        SpoonDraggable spoon = eventData.pointerDrag?.GetComponent<SpoonDraggable>();
        if (spoon != null)
        {
            currentAmount += spoon.spoonValue;
            UpdateUI();

            // Check Win Condition
            if (currentAmount >= targetAmount)
            {
                WinGame();
            }
        }
    }

    private void UpdateUI()
    {
        if (currentAmountText != null)
            currentAmountText.text = $"Added: {currentAmount} tb";

        if (targetAmountText != null)
            targetAmountText.text = $"Target: {targetAmount} tb";
    }

    private void WinGame()
    {
        isGameActive = false;
        Debug.Log("Success! Target seasoning reached.");
    }

    private void TimeOut()
    {
        isGameActive = false;
        Debug.Log("Time's up! Failed to season in time.");
    }
}