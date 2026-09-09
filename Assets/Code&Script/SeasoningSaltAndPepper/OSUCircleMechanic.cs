using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OSUTargetItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform approachRing;
    [SerializeField] private Button targetButton;
    [SerializeField] private TextMeshProUGUI numberText;

    private float shrinkDuration;
    private float graceWindow;
    private float currentStartScale;
    private float startDelay; // Delay before this specific ring starts shrinking

    private float timer = 0f;
    private bool isActivated = false;
    private bool isFinished = false;
    private System.Action<OSUTargetItem, int> onCompleteCallback;

    public void SetupTarget(
        int sequenceNumber,
        float duration,
        float grace,
        float startScale,
        float delay,
        System.Action<OSUTargetItem, int> callback)
    {
        shrinkDuration = duration;
        graceWindow = grace;
        currentStartScale = startScale;
        startDelay = delay;
        onCompleteCallback = callback;

        if (numberText != null)
            numberText.text = sequenceNumber.ToString();

        if (approachRing != null)
            approachRing.localScale = Vector3.one * currentStartScale;

        if (targetButton == null)
            targetButton = GetComponentInChildren<Button>();

        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(OnTargetClicked);

        timer = 0f;
        isActivated = false;
        isFinished = false;
    }

    private void Update()
    {
        if (isFinished) return;

        timer += Time.deltaTime;

        // 1. Wait for sequence start delay
        if (!isActivated)
        {
            if (timer >= startDelay)
            {
                isActivated = true;
                timer = 0f; // Reset timer to start shrink duration from zero
            }
            return;
        }

        // 2. Shrink approach ring
        float progress = Mathf.Clamp01(timer / shrinkDuration);
        float scale = Mathf.Lerp(currentStartScale, 1.0f, progress);

        if (approachRing != null)
            approachRing.localScale = Vector3.one * scale;

        // 3. Auto-miss if timer runs out after grace window
        if (timer >= shrinkDuration + graceWindow)
        {
            isFinished = true;
            Debug.Log($"Target {numberText.text} MISS! (Too Late)");
            FinishTarget(0);
        }
    }

    private void OnTargetClicked()
    {
        if (!isActivated || isFinished) return;

        isFinished = true;

        // PERFECT: Player tapped after ring fully covered target circle
        if (timer >= shrinkDuration && timer < shrinkDuration + graceWindow)
        {
            Debug.Log($"Target {numberText.text} PERFECT! (+300)");
            FinishTarget(300);
        }
        // GOOD: Player tapped early while ring was shrinking
        else if (timer < shrinkDuration)
        {
            Debug.Log($"Target {numberText.text} GOOD! (+100)");
            FinishTarget(100);
        }
    }

    private void FinishTarget(int score)
    {
        onCompleteCallback?.Invoke(this, score);
        Destroy(gameObject);
    }
}