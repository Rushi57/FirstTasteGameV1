using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MeltAndVanish : MonoBehaviour
{
    [Header("Timing")]
    public float delayBeforeMelt = 0.3f;   // sits in the pan for a moment first
    public float meltDuration = 2.0f;

    [Header("Look")]
    public Vector2 endScale = new Vector2(1.35f, 0.25f);   // wider + flatter
    public Color meltedTint = new Color(1f, 0.85f, 0.3f, 1f); // golden, oily
    [Range(0f, 1f)] public float startFadeAt = 0.55f;       // when alpha starts dropping

    [Header("Optional puddle")]
    [Tooltip("A melted-butter blob sprite. Leave empty to skip.")]
    public Sprite puddleSprite;
    public Vector2 puddleSize = new Vector2(140f, 60f);

    private Image img;
    private Vector3 startScale;
    private Color startColor;
    private bool melting;

    public event System.Action OnMeltFinished;
    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public void BeginMelt()
    {
        if (melting) return;
        melting = true;

        // can't be grabbed while melting
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;
        else img.raycastTarget = false;

        StartCoroutine(MeltRoutine());
    }

    private IEnumerator MeltRoutine()
    {
        startScale = transform.localScale;
        startColor = img.color;

        yield return new WaitForSeconds(delayBeforeMelt);

        Image puddle = CreatePuddle();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / meltDuration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            // squash and spread
            transform.localScale = new Vector3(
                Mathf.Lerp(startScale.x, startScale.x * endScale.x, k),
                Mathf.Lerp(startScale.y, startScale.y * endScale.y, k),
                1f);

            // tint toward golden, then fade out
            Color c = Color.Lerp(startColor, meltedTint, k);
            float fade = Mathf.InverseLerp(startFadeAt, 1f, t);
            c.a = Mathf.Lerp(startColor.a, 0f, fade);
            img.color = c;

            // puddle grows in, then fades with it
            if (puddle != null)
            {
                float pa = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI); // in then out
                puddle.color = new Color(1f, 0.9f, 0.4f, pa * 0.8f);
                puddle.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.1f, k);
            }
            yield return null;
        }

        if (puddle != null) Destroy(puddle.gameObject);
        OnMeltFinished?.Invoke();
        gameObject.SetActive(false);
    }

    private Image CreatePuddle()
    {
        if (puddleSprite == null) return null;

        var go = new GameObject("MeltPuddle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform.parent, false);
        go.transform.SetSiblingIndex(transform.GetSiblingIndex()); // behind the butter
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = ((RectTransform)transform).anchoredPosition;
        rt.sizeDelta = puddleSize;

        var pi = go.GetComponent<Image>();
        pi.sprite = puddleSprite;
        pi.raycastTarget = false;
        pi.color = new Color(1f, 0.9f, 0.4f, 0f);
        return pi;
    }
}