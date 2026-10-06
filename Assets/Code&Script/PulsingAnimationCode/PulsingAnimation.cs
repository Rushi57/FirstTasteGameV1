using UnityEngine;

public class PulsingAnimation : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("How fast the object pulse")]
    public float pulseSpeed = 3.0f;

    [Tooltip("How much the Object scale relative to its starting size")]
    public float pulseAmount = 0.1f;

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }
    private void Update()
    {
        float scaleFactor = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        transform.localScale = initialScale + (initialScale *  scaleFactor);
    }
    private void OnDisable()
    {
        transform.localScale = initialScale;
    }
}
