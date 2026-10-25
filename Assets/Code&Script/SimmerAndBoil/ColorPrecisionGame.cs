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
    public float headstartDelay = 2f;
    public float retryDelay = 1.5f; // how long "BAD!" is shown before restarting

    private bool isSpinning = false;
    private bool canStop = false;
    private Coroutine activeRoutine;

    private void OnEnable()
    {
        StartRound();
    }

    // Single entry point so routines never overlap
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

        yellowImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
        greenImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));

        if (resultText != null) resultText.text = "GET READY!";

        yield return new WaitForSeconds(headstartDelay);

        if (resultText != null) resultText.text = "GO!";

        isSpinning = true;
        canStop = true;
        SFXManager.Instance?.StartClock();

        activeRoutine = null;
    }

    private IEnumerator RetryAfterBad(string message)
    {
        // Input stays locked (canStop is already false)
        if (resultText != null) resultText.text = message;

        yield return new WaitForSeconds(retryDelay);

        // Restart the whole mechanic (re-randomizes slices + headstart)
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

        if (IsPointInSlice(point, greenStart, greenImage.fillAmount))
        {
            resultText.text = "VERY GOOD!";
            ScoreManager.Instance?.ReportResult(ResultQuality.VeryGood);
        }
        else if (IsPointInSlice(point, yellowStart, yellowImage.fillAmount))
        {
            resultText.text = "GOOD";
            ScoreManager.Instance?.ReportResult(ResultQuality.Good);
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
            // Tutorial: no deduction, just try again
            TutorialManager.Instance.ReportMistake();
            

            if(gameObject .activeInHierarchy)
                activeRoutine = StartCoroutine(RetryAfterBad("TRY AGAIN!"));

        }
        else
        {
            // Normal play: deduct points + heart, then retry
            ScoreManager.Instance?.ReportResult(ResultQuality.Bad);

            // If the heart loss ended the game and this object was
            // disabled, the coroutine below simply won't run.
            if (gameObject.activeInHierarchy)
                activeRoutine = StartCoroutine(RetryAfterBad("BAD!"));
        }
    }

    // TODO: connect this to your real tutorial system
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
}