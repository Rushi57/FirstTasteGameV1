using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ColorPrecisionGame : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform hand;
    public RectTransform wheelContainer; // The 'Wheel' object
    public Image redImage, yellowImage, greenImage;
    public TextMeshProUGUI resultText;

    [Header("Settings")]
    public float rotateSpeed = 200f;
    public float headstartDelay = 2f; // 2-second headstart before hand moves

    private bool isSpinning = false;

    // Called automatically whenever the script/GameObject is set active
    private void OnEnable()
    {
        StartCoroutine(StartWithHeadstart());
    }

    private IEnumerator StartWithHeadstart()
    {
        isSpinning = false;

        // Reset hand position to top
        hand.localEulerAngles = Vector3.zero;

        // Randomize slice rotations immediately so player can see them
        yellowImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
        greenImage.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));

        if (resultText != null)
        {
            resultText.text = "GET READY!";
        }

        // Wait for 2 seconds (player headstart)
        yield return new WaitForSeconds(headstartDelay);

        if (resultText != null)
        {
            resultText.text = "GO!";
        }

        isSpinning = true;
    }

    void Update()
    {
        if (isSpinning)
        {
            // Rotate the hand clockwise (Negative Z)
            hand.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
        }
    }

    public void StopHand()
    {
        if (!isSpinning) return;
        isSpinning = false;

        // Get hand rotation relative to the wheel container
        float angle = hand.localEulerAngles.z;
        if (angle < 0) angle += 360;

        // Convert rotation to a 0-1 "clock" value:
        float normalizedPoint = 1f - (angle / 360f);

        // Handle the 'wrap around' since 1.0 and 0.0 are both the Top
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

        // Check if hand is inside Green or Yellow "zones"
        if (IsPointInSlice(point, greenStart, greenImage.fillAmount))
        {
            resultText.text = "<color=green>VERY GOOD!</color>";
        }
        else if (IsPointInSlice(point, yellowStart, yellowImage.fillAmount))
        {
            resultText.text = "<color=yellow>GOOD</color>";
        }
        else
        {
            resultText.text = "<color=red>BAD!</color>";
        }
    }

    // Helper to handle slices that cross the "12 o'clock" line
    bool IsPointInSlice(float point, float start, float fill)
    {
        float end = (start + fill) % 1f;
        if (start < end) return point >= start && point <= end;
        else return point >= start || point <= end;
    }
}