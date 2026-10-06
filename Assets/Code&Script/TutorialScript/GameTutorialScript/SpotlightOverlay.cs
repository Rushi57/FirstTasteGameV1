using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Purely visual: dims the background and draws a pulsing ring around the current
/// tutorial target, plus an optional secondary ring around a drop zone. Does NOT
/// block input by itself - that's TutorialInputBlocker's job. Keep dimBackground's
/// Image.raycastTarget UNCHECKED so it never intercepts touches.
/// </summary>
public class SpotlightOverlay : MonoBehaviour
{
    [SerializeField] private Image dimBackground;      // raycastTarget = OFF, purely visual dim
    [SerializeField] private RectTransform ring;        // highlight ring around the target
    [SerializeField] private RectTransform secondaryRing; // optional ring around a drop zone
    [SerializeField] private Vector2 padding = new Vector2(30f, 30f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.08f;

    private Vector2 ringBaseSize;
    private Coroutine pulseRoutine;

    public void Focus(RectTransform target)
    {
        gameObject.SetActive(true);
        if (dimBackground != null) dimBackground.enabled = true;
        secondaryRing.gameObject.SetActive(false);

        if (target == null)
        {
            ring.gameObject.SetActive(false);
            return;
        }

        ring.gameObject.SetActive(true);
        ring.position = target.position;
        ringBaseSize = target.rect.size + padding;
        ring.sizeDelta = ringBaseSize;

        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(Pulse());
    }

    public void ShowSecondaryHighlight(RectTransform dropZone)
    {
        if (dropZone == null) return;
        secondaryRing.gameObject.SetActive(true);
        secondaryRing.position = dropZone.position;
        secondaryRing.sizeDelta = dropZone.rect.size + padding;
    }

    IEnumerator Pulse()
    {
        while (true)
        {
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            ring.sizeDelta = ringBaseSize * scale;
            yield return null;
        }
    }

    public void Clear()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        gameObject.SetActive(false);
    }
}