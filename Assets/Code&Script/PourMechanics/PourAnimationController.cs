using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Put this on CupImage, TbpsImage, and the salt/pepper spoon animation
/// objects (all children of AnimationGameObject, all inactive by default).
/// No Animator needed - this rotates the RectTransform directly on the Z axis.
/// </summary>
public class PourAnimationController : MonoBehaviour
{
    [Header("Rotation (degrees, Z axis)")]
    public float idleAngle = 0f;
    public float tiltAngle = 52.958f;

    [Header("Timing (seconds)")]
    public float tiltDuration = 0.35f;
    public float holdDuration = 0.4f;   // pause at full tilt before returning
    public float returnDuration = 0.35f;


    [Header("Easing")]
    [Tooltip("Evaluated 0->1 over each phase. Default (linear-ish ease) works for most pours.")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Visual")]
    [Tooltip("The Image on this object, tinted to match the seasoning.")]
    public Image image;

    private RectTransform rect;
    private Coroutine running;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (image == null) image = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        if (image != null) image.color = color;
    }

    /// <summary>Activates this object, resets to Idle, tints it, then tilts -> holds -> returns.</summary>
    public void PlayPourAnimation(Color color, Action onComplete, bool hideAfter = true)
    {
        gameObject.SetActive(true);
        SetAngle(idleAngle);

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(PlaySequence(onComplete, hideAfter));
    }

    private IEnumerator PlaySequence(Action onComplete, bool hideAfter)
    {
        Debug.Log($"[PourAnim] {name}: tilting {idleAngle}\u00b0 -> {tiltAngle}\u00b0 over {tiltDuration}s");
        yield return RotateOverTime(idleAngle, tiltAngle, tiltDuration);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        Debug.Log($"[PourAnim] {name}: returning {tiltAngle}\u00b0 -> {idleAngle}\u00b0 over {returnDuration}s");
        yield return RotateOverTime(tiltAngle, idleAngle, returnDuration);

        Debug.Log($"[PourAnim] {name}: Tilt + Return finished.");

        running = null;
        if (hideAfter) gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator RotateOverTime(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAngle(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = easeCurve.Evaluate(Mathf.Clamp01(t / duration));
            SetAngle(Mathf.LerpAngle(from, to, p));
            yield return null;
        }
        SetAngle(to);
    }

    private void SetAngle(float z)
    {
        if (rect == null) rect = transform as RectTransform;
        Vector3 e = rect.localEulerAngles;
        e.z = z;
        rect.localEulerAngles = e;
    }

    public void ReturnToIdle()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        SetAngle(idleAngle);
    }

    public void Hide()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        SetAngle(idleAngle);
        gameObject.SetActive(false);
    }
}