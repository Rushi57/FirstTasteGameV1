using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Put this on CupImage AND on TbpsImage (one instance per object), both
/// children of AnimationGameObject, both inactive by default.
/// Each needs its own Animator with an Idle/Tilt/Return set of states.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PourAnimationController : MonoBehaviour
{
    [Header("Animator state names")]
    public string idleState = "TbpsAnimIdle";
    public string tiltState = "TbpsAnimTilt";
    public string returnState = "TbpsAnimReturn";

    [Header("Speed")]
    [Tooltip("1 = normal speed, 2 = twice as fast, 0.5 = half speed.")]
    public float tiltSpeed = 1f;
    public float returnSpeed = 1f;

    [Header("Visual")]
    [Tooltip("The Image on this object, tinted to match the seasoning.")]
    public Image image;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (image == null) image = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        if (image != null) image.color = color;
    }

    /// <summary>Activates this object, resets to Idle, tints it, then plays Tilt -> Return.</summary>
    public void PlayPourAnimation(Color color, Action onComplete, bool hideAfter = true)
    {
        gameObject.SetActive(true);
        SetColor(color);

        StopAllCoroutines();
        StartCoroutine(PlaySequence(onComplete, hideAfter));
    }

    private IEnumerator PlaySequence(Action onComplete, bool hideAfter)
    {
        animator.speed = 1f;
        animator.Play(idleState, 0, 0f);
        yield return null; // let the Animator settle into Idle first

        yield return PlayAndWait(tiltState, tiltSpeed);
        yield return PlayAndWait(returnState, returnSpeed);

        animator.speed = 1f; // reset so Idle plays at normal speed afterward
        Debug.Log($"[PourAnim] {name}: Tilt + Return finished.");

        if (hideAfter) gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator PlayAndWait(string stateName, float speed)
    {
        animator.speed = Mathf.Max(0.01f, speed);
        animator.Play(stateName, 0, 0f);
        Debug.Log($"[PourAnim] {name}: playing '{stateName}' at {speed}x");

        yield return null; // one frame so the Animator enters the state before we read its length
        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length / animator.speed);
    }

    public void ReturnToIdle()
    {
        StopAllCoroutines();
        animator.speed = 1f;
        animator.Play(idleState, 0, 0f);
    }

    public void Hide()
    {
        StopAllCoroutines();
        animator.speed = 1f;
        gameObject.SetActive(false);
    }
}