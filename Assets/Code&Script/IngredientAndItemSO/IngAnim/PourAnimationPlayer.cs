using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Code-driven pour animation - no Animator needed.
/// Position priority when playing:
///   1. Position passed in (e.g. from IngredientData.pourPositions)
///   2. pourTarget's anchoredPosition (if assigned)
///   3. The pitcher's position as set up in the scene
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
    [Tooltip("Where the pitcher starts/ends, relative to its pour position.")]
    public Vector2 enterOffset = new Vector2(200f, 120f);

    [Tooltip("Tilt angle (Z rotation) while pouring. Flip the sign if it tilts the wrong way.")]
    public float pourAngle = 80f;

    [Tooltip("Optional fallback: an empty RectTransform (same parent as the pitcher) marking the pour spot. Used when the ingredient has no pour position.")]
    public RectTransform pourTarget;

    [Tooltip("Pause at full tilt BEFORE the stream starts (pitcher is idle at the pour angle).")]
    public float idleBeforePour = 0.15f;

    [Tooltip("Pause at full tilt AFTER the stream stops, before tilting back.")]
    public float idleAfterPour = 0.15f;

    [Header("Timing (seconds)")]
    public float moveInTime = 0.4f;
    public float tiltTime = 0.35f;
    public float holdTime = 0.8f;
    public float tiltBackTime = 0.3f;
    public float moveOutTime = 0.4f;

    [Header("Easing")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public UnityEvent onPourFinished;

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

    /// <summary>
    /// Plays the pour at the position stored on the ingredient (IngredientData.pourPositions).
    /// Falls back to pourTarget, then the scene position, if the ingredient has none.
    /// </summary>
    public bool Play(Action onFinished, IngredientData data, string targetName = "Pan")
    {
        Vector2? pos = null;
        if (data != null && data.TryGetPourPosition(targetName, out Vector2 p))
            pos = p;
        return Play(onFinished, pos);
    }

    /// <summary>Returns false if already playing. onFinished fires after the pitcher is hidden again.</summary>
    public bool Play(Action onFinished, Vector2? positionOverride = null)
    {
        if (IsPlaying || pitcher == null) return false;

        Vector2 pos = positionOverride
            ?? (pourTarget != null ? pourTarget.anchoredPosition : pourPosition);

        Debug.Log($"[Water] Play called. Override: {(positionOverride.HasValue ? positionOverride.Value.ToString() : "none")} -> using {pos}");

        TutorialManager.Instance?.BeginBusy();
        StartCoroutine(Run(onFinished, pos));
        return true;
    }

    private IEnumerator Run(Action onFinished, Vector2 pourPos)
    {
        Debug.Log("[Water] pour animation started");
        IsPlaying = true;

        Vector2 offscreen = pourPos + enterOffset;

        pitcher.anchoredPosition = offscreen;
        pitcher.localRotation = Quaternion.identity;
        pitcher.gameObject.SetActive(true);

        // 1. Slide in over the target
        yield return Tween(moveInTime, t =>
            pitcher.anchoredPosition = Vector2.LerpUnclamped(offscreen, pourPos, t));

        // 2. Tilt to the pour angle (no stream yet)
        yield return Tween(tiltTime, t =>
            pitcher.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(0f, pourAngle, t)));

        // 2b. Idle at full tilt before the water comes out
        if (idleBeforePour > 0f) yield return new WaitForSeconds(idleBeforePour);

        // 3. Stream runs only while the pitcher is held at the pour angle
        if (pourEffect != null) pourEffect.SetActive(true);
        SFXManager.Instance?.StartPour();
        yield return new WaitForSeconds(holdTime);
        if (pourEffect != null) pourEffect.SetActive(false);
        SFXManager.Instance?.StopPour();

        // 3b. Idle at full tilt after the water stops
        if (idleAfterPour > 0f) yield return new WaitForSeconds(idleAfterPour);

        // 4. Tilt back
        yield return Tween(tiltBackTime, t =>
            pitcher.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(pourAngle, 0f, t)));

        // 5. Slide out
        yield return Tween(moveOutTime, t =>
            pitcher.anchoredPosition = Vector2.LerpUnclamped(pourPos, offscreen, t));

        // Reset and hide (back to the original scene pose)
        pitcher.localRotation = Quaternion.identity;
        pitcher.anchoredPosition = pourPosition;
        pitcher.gameObject.SetActive(false);

        IsPlaying = false;
        TutorialManager.Instance?.EndBusy();
        onFinished?.Invoke();
        onPourFinished?.Invoke();
        Debug.Log("[Water] pour finished");
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
            SFXManager.Instance?.StopPour();
            TutorialManager.Instance?.EndBusy();
        }
    }

    public void Configure(Sprite pitcherSprite, Sprite streamSprite, Color streamColor)
    {
        if (pitcher != null && pitcherSprite != null)
        {
            Image img = pitcher.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = pitcherSprite;
                img.preserveAspect = true;
            }
        }

        if (pourEffect != null)
        {
            Image stream = pourEffect.GetComponent<Image>();
            if (stream != null)
            {
                if (streamSprite != null) stream.sprite = streamSprite;
                stream.color = streamColor;
            }
        }
    }
}