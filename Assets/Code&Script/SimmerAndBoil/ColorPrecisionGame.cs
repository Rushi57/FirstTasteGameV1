using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ColorPrecisionGame : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform hand;
    public RectTransform wheelContainer;
    public Image redImage, yellowImage, greenImage;
    public TextMeshProUGUI resultText;

    [Header("Settings")]
    public float rotateSpeed = 200f;
    public float countSoundDelay = 1f;
    public float headstartDelay = 2f;
    public float retryDelay = 1.5f; 

    [Header("Hit Detection")]
    [Tooltip("Half the arrow head's width, in degrees. 0 = only the tip line counts.")]
    public float handHalfWidthDeg = 8f;
    [Tooltip("ON = the whole arrow head must be inside the color. OFF = any overlap counts.")]
    public bool requireWholeHead = true;

    [Header("Zone Sizes (fraction of the circle)")]
    [Range(0.02f, 0.5f)] public float greenFill = 0.12f;   
    [Range(0.02f, 0.6f)] public float yellowFill = 0.25f; 

    private bool isSpinning = false;
    private bool canStop = false;
    private Coroutine activeRoutine;
    public event System.Action OnSucceeded;
    public event System.Action OnRoundStarting;
    public event System.Action OnHandReleased;
    public event System.Action OnHandStopped;
    public bool CanStop => canStop;
    private void OnEnable()
    {
        StartRound();
    }

    private void StartRound()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StartWithHeadstart());
    }

    private IEnumerator StartWithHeadstart()
    {
        isSpinning = false;
        canStop = false;

        hand.localEulerAngles = Vector3.zero;
        greenImage.fillAmount = greenFill;
        yellowImage.fillAmount = yellowFill;

        yellowImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
        greenImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));

        if (resultText != null) resultText.text = "GET READY!";

        yield return new WaitForSeconds(countSoundDelay);     

        SFXManager.Instance?.PlayCountHandStart();           

        yield return new WaitForSeconds(headstartDelay);      
        if (resultText != null) resultText.text = "GO!";

        isSpinning = true;
        canStop = true;
        SFXManager.Instance?.StartClock();

        activeRoutine = null;
    }

    private IEnumerator RetryAfterBad(string message)
    {

        if (resultText != null) resultText.text = message;

        yield return new WaitForSeconds(retryDelay);

        activeRoutine = null;
        StartRound();
    }

    void Update()
    {
        if (isSpinning)
        {
            hand.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
        }
    }

    public void StopHand()
    {
        if (!canStop || !isSpinning) return;

        canStop = false;
        isSpinning = false;
        SFXManager.Instance?.StopClock();
        OnHandStopped?.Invoke();

        float angle = hand.localEulerAngles.z;
        if (angle < 0) angle += 360;

        float normalizedPoint = 1f - (angle / 360f);
        if (normalizedPoint >= 1.0f) normalizedPoint = 0f;

        CheckResult(normalizedPoint);
    }

    void CheckResult(float point)
    {
        float greenStart = greenImage.rectTransform.localEulerAngles.z / 360f;
        greenStart = 1f - greenStart;
        if (greenStart >= 1f) greenStart -= 1f;

        float yellowStart = yellowImage.rectTransform.localEulerAngles.z / 360f;
        yellowStart = 1f - yellowStart;
        if (yellowStart >= 1f) yellowStart -= 1f;


        float half = handHalfWidthDeg / 360f;
        Debug.Log($"[ColorPrecision] point={point:F3} left={Mathf.Repeat(point - half, 1f):F3} right={Mathf.Repeat(point + half, 1f):F3} " +
                  $"| green {greenStart:F3} to {Mathf.Repeat(greenStart + greenImage.fillAmount, 1f):F3} " +
                  $"| greenFill={greenImage.fillAmount:F3} greenFillConfig={greenFill:F3} " +
                  $"| tutorialActive={IsTutorialActive()}");

        if (HandHitsSlice(point, greenStart, greenImage.fillAmount, true))
        {
            resultText.text = "VERY GOOD!";
            ScoreManager.Instance?.ReportResult(ResultQuality.VeryGood);
            OnSucceeded?.Invoke();
        }
        else if (HandHitsSlice(point, greenStart, greenImage.fillAmount, false)
      || HandHitsSlice(point, yellowStart, yellowImage.fillAmount, false))
        {
            resultText.text = "GOOD";
            ScoreManager.Instance?.ReportResult(ResultQuality.Good);
            OnSucceeded?.Invoke();
        }
        else
        {
            HandleBad();
        }
    }

    private void HandleBad()
    {
        if (IsTutorialActive())
        {
         
            TutorialManager.Instance.ReportMistake();
            

            if(gameObject .activeInHierarchy)
                activeRoutine = StartCoroutine(RetryAfterBad("TRY AGAIN!"));

        }
        else
        {
    
            ScoreManager.Instance?.ReportResult(ResultQuality.Bad);
            if (gameObject.activeInHierarchy)
                activeRoutine = StartCoroutine(RetryAfterBad("BAD!"));
        }
    }

    private bool IsTutorialActive()
    {
        return TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
    }

    bool IsPointInSlice(float point, float start, float fill)
    {
        float end = (start + fill) % 1f;
        if (start < end) return point >= start && point <= end;
        else return point >= start || point <= end;
    }

    bool HandHitsSlice(float point, float start, float fill, bool wholeHead)
    {
        float half = handHalfWidthDeg / 360f;
        float left = Mathf.Repeat(point - half, 1f);
        float right = Mathf.Repeat(point + half, 1f);

        bool leftIn = IsPointInSlice(left, start, fill);
        bool centerIn = IsPointInSlice(point, start, fill);
        bool rightIn = IsPointInSlice(right, start, fill);

        return wholeHead
            ? (leftIn && centerIn && rightIn)
            : (leftIn || centerIn || rightIn);
    }

    public void SetSpeed(float speed)
    {
        rotateSpeed = speed;
    }

    private void OnDisable()
    {
        isSpinning = false;
        canStop = false;
        activeRoutine = null;
        SFXManager.Instance?.StopClock();
    }
    private void OnValidate()
    {
        if (greenImage != null) greenImage.fillAmount = greenFill;
        if (yellowImage != null) yellowImage.fillAmount = yellowFill;
    }
}