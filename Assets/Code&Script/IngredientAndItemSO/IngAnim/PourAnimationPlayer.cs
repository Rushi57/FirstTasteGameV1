using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Code-driven pour animation - no Animator needed.
/// The pitcher's position in the scene is the POUR position (over the pan).
/// It slides in from an offset, tilts, holds, tilts back, slides out, then hides.
/// </summary>
public class PourAnimationPlayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("'Pitcher with water' - keep it disabled in the Hierarchy.")]
    public RectTransform pitcher;

    [Tooltip("Optional: a water stream / splash object shown only while pouring.")]
    public GameObject pourEffect;

    [Header("Motion")]
    [Tooltip("Where the pitcher starts/ends, relative to its pour position in the scene.")]
    public Vector2 enterOffset = new Vector2(200f, 120f);

    [Tooltip("Tilt angle (Z rotation) while pouring. Flip the sign if it tilts the wrong way.")]
    public float pourAngle = 80f;

    [Tooltip("Optional: the pitcher's pour position snaps to this object (e.g. an empty RectTransform above the pan).")]
    public RectTransform pourTarget;

    [Header("Timing (seconds)")]
    public float moveInTime = 0.4f;
    public float tiltTime = 0.35f;
    public float holdTime = 0.8f;
    public float tiltBackTime = 0.3f;
    public float moveOutTime = 0.4f;

    [Header("Easing")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public bool IsPlaying { get; private set; }

    private Vector2 pourPosition;

    void Awake()
    {
        if (pitcher == null) return;

        // Remember the pose you set up in the scene, then hide it.
        pourPosition = pitcher.anchoredPosition;

        // An Animator would fight this script, so switch it off if one exists.
        Animator anim = pitcher.GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        pitcher.gameObject.SetActive(false);
        if (pourEffect != null) pourEffect.SetActive(false);
    }

    /// <summary>Returns false if already playing. onFinished fires after the pitcher is hidden again.</summary>
    public bool Play(Action onFinished)
    {
        if (IsPlaying || pitcher == null) return false;
        TutorialManager.Instance?.BeginBusy();
        StartCoroutine(Run(onFinished));
        return true;
    }

    private IEnumerator Run(Action onFinished)
    {
        IsPlaying = true;

        Vector2 offscreen = pourPosition + enterOffset;

        pitcher.anchoredPosition = offscreen;
        pitcher.localRotation = Quaternion.identity;
        pitcher.gameObject.SetActive(true);

        // 1. Slide in over the pan
        yield return Tween(moveInTime, t =>
            pitcher.anchoredPosition = Vector2.LerpUnclamped(offscreen, pourPosition, t));

        // 2. Tilt to pour
        if (pourEffect != null) pourEffect.SetActive(true);
        yield return Tween(tiltTime, t =>
            pitcher.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(0f, pourAngle, t)));

        // 3. Hold while the water pours
        yield return new WaitForSeconds(holdTime);

        // 4. Tilt back
        if (pourEffect != null) pourEffect.SetActive(false);
        yield return Tween(tiltBackTime, t =>
            pitcher.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(pourAngle, 0f, t)));

        // 5. Slide out
        yield return Tween(moveOutTime, t =>
            pitcher.anchoredPosition = Vector2.LerpUnclamped(pourPosition, offscreen, t));

        // Reset and hide
        pitcher.localRotation = Quaternion.identity;
        pitcher.anchoredPosition = pourPosition;
        pitcher.gameObject.SetActive(false);

        IsPlaying = false;
        TutorialManager.Instance?.EndBusy();
        onFinished?.Invoke();
    }



    private IEnumerator Tween(float duration, Action<float> apply)
    {
        if (duration <= 0f) { apply(1f); yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            apply(ease.Evaluate(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        apply(1f);
    }
    void OnDisable()
    {
        if (IsPlaying)
        {
            IsPlaying = false;
            TutorialManager.Instance?.EndBusy();
        }
    }
}