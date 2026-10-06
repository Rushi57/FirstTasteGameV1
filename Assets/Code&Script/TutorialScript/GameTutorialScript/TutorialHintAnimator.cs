using System.Collections;
using UnityEngine;

/// <summary>
/// Animated hand-icon hint. Pulses in place for tap gestures, or slides back and
/// forth between two world positions for drag gestures (e.g. shelf -> chopping board).
/// </summary>
public class TutorialHintAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform handIcon;
    [SerializeField] private float tapPulseSpeed = 3f;
    [SerializeField] private float tapPulseAmount = 0.2f;
    [SerializeField] private float dragSpeed = 0.6f;

    private Coroutine routine;

    public void PlayTapHint(RectTransform target)
    {
        Stop();
        if (target == null) return;

        handIcon.gameObject.SetActive(true);
        handIcon.position = target.position;
        routine = StartCoroutine(TapLoop());
    }

    public void PlayDragHint(Vector3 fromWorldPos, Vector3 toWorldPos)
    {
        Stop();
        handIcon.gameObject.SetActive(true);
        routine = StartCoroutine(DragLoop(fromWorldPos, toWorldPos));
    }

    IEnumerator TapLoop()
    {
        while (true)
        {
            float s = 1f + Mathf.PingPong(Time.time * tapPulseSpeed, tapPulseAmount);
            handIcon.localScale = Vector3.one * s;
            yield return null;
        }
    }

    IEnumerator DragLoop(Vector3 from, Vector3 to)
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * dragSpeed, 1f);
            float eased = t * t * (3f - 2f * t); // smoothstep
            handIcon.position = Vector3.Lerp(from, to, eased);
            yield return null;
        }
    }

    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        if (handIcon != null) handIcon.gameObject.SetActive(false);
    }
}