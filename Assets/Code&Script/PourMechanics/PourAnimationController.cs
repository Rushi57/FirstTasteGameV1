using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class PourAnimationController : MonoBehaviour
{
    [Header("Rotation (degrees, Z axis)")]
    public float idleAngle = 0f;
    public float tiltAngle = 52.958f;

    [Header("Timing (seconds)")]
    public float tiltDuration = 0.35f;
    public float holdDuration = 0.4f;  
    public float returnDuration = 0.35f;

    [Header("Liquid sprites")]
    public Sprite seasoningStreamSprite;  
   

    [Header("Easing")]
    [Tooltip("Evaluated 0->1 over each phase. Default (linear-ish ease) works for most pours.")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Visual")]
    public Image image;
    [Tooltip("Optional child Image that represents the liquid. Gets the seasoning color; the cup/spoon stays untouched.")]
    public Image liquidImage;

    private RectTransform rect;
    private Coroutine running;

    [Tooltip("Stream disappears the moment the cup starts tilting back.")]
    public bool hideStreamOnReturn = true;
    private bool returning;

    private bool busyRegistered;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (image == null) image = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.color = color;
    }

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null) return;
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.sprite = sprite;
    }


    public void PlayPourAnimation(Color color, Action onComplete, bool hideAfter = true)
    {
        SetBusy(true);
        returning = false;  
        gameObject.SetActive(true);
        if (liquidImage != null)
        {
            liquidImage.gameObject.SetActive(true);
            if (seasoningStreamSprite != null) liquidImage.sprite = seasoningStreamSprite;
        }
        SetAngle(idleAngle);
        SetLiquidColor(color);
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(PlaySequence(onComplete, hideAfter));
    }

    private IEnumerator PlaySequence(Action onComplete, bool hideAfter)
    {
        Debug.Log($"[PourAnim] {name}: tilting {idleAngle}\u00b0 -> {tiltAngle}\u00b0 over {tiltDuration}s");
        yield return RotateOverTime(idleAngle, tiltAngle, tiltDuration);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        returning = true;
        if (liquidImage != null)
        {
            if (hideStreamOnReturn) liquidImage.gameObject.SetActive(false);
            else liquidImage.fillAmount = 1f;
        }

        Debug.Log($"[PourAnim] {name}: returning {tiltAngle}\u00b0 -> {idleAngle}\u00b0 over {returnDuration}s");
        yield return RotateOverTime(tiltAngle, idleAngle, returnDuration);

        Debug.Log($"[PourAnim] {name}: Tilt + Return finished.");

        running = null;
        if (hideAfter)
        {
            if (liquidImage != null) liquidImage.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
        onComplete?.Invoke();
        SetBusy(false);
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

    public void SetLiquidColor(Color color)
    {
        if (liquidImage != null) liquidImage.color = color;
    }

    private void SetAngle(float z)
    {
        if (rect == null) rect = transform as RectTransform;
        Vector3 e = rect.localEulerAngles;
        e.z = z;
        rect.localEulerAngles = e;

        if (liquidImage != null)
        {
            liquidImage.rectTransform.rotation = Quaternion.identity;

            if (!returning)  
            {
                float tiltProgress = Mathf.InverseLerp(idleAngle, tiltAngle, z);
                liquidImage.fillAmount = Mathf.Clamp01((tiltProgress - 0.6f) / 0.4f);
            }
        }
    }

    public void ReturnToIdle()
    {
        SetBusy(false);
        returning = false;
        if (running != null) StopCoroutine(running);
        running = null;
        SetAngle(idleAngle);
    }

    public void Hide()
    {
        SetBusy(false);
        returning = false;
        if (running != null) StopCoroutine(running);
        running = null;
        SetAngle(idleAngle);
        if (liquidImage != null) liquidImage.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
  

   
    private void SetBusy(bool on)
    {
        if (on == busyRegistered) return;
        busyRegistered = on;
        if (on) TutorialManager.Instance?.BeginBusy();
        else TutorialManager.Instance?.EndBusy();
    }

    private void OnDisable() => SetBusy(false);  
}