using UnityEngine;
using UnityEngine.UI;

public class TutorialPulse : MonoBehaviour
{
    [Tooltip("Smallest scale multiplier during the pulse.")]
    public float minScale = 1.1f;
    [Tooltip("Largest scale multiplier during the pulse.")]
    public float maxScale = 1.3f;
    [Tooltip("How fast the pulsing is.")]
    public float speed = 2.5f;

    private float outlineMin = 4f;
    private float outlineMax = 8f;

    private Vector3 baseScale;
    private Image img;
    private RectTransform rt;
    private Material savedMat;
    private Material mat;

    private void Awake()
    {
        img = GetComponent<Image>();
        rt = transform as RectTransform;
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    /// <summary>Called by TutorialManager right after AddComponent.</summary>
    public void SetOutline(Material source, float min, float max)
    {
        outlineMin = min;
        outlineMax = max;

        RemoveOutline();
        if (source == null || img == null) return;   // no Image on this object -> scale pulse only

        savedMat = img.material;
        mat = new Material(source);                  // per-object copy
        img.material = mat;
    }

    private void RemoveOutline()
    {
        if (mat == null) return;

        if (img != null) img.material = savedMat;
        Destroy(mat);
        mat = null;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, t);

        if (mat != null)
        {
            float px = Mathf.Lerp(outlineMin, outlineMax, t);
            Vector2 size = rt.rect.size;
            mat.SetVector("_PadLocal", new Vector4(px, px, 0, 0));
            mat.SetVector("_PadUV", new Vector4(px / size.x, px / size.y, 0, 0));
        }
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
        RemoveOutline();
    }
}