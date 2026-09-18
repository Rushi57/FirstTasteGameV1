using UnityEngine;

public class TutorialPulse : MonoBehaviour
{
    [Tooltip("Smallest scale multiplier during the pulse.")]
    public float minScale = 1f;

    [Tooltip("Large Scale multiplyer")]
    public float maxScale = 1.5f;

    [Tooltip("How fast the pulsing")]
    public float speed = 2.5f;

    private Vector3 baseScale;

    private void Awake()
    {
         baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = baseScale * scale;

    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
    }
}
