using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragHighlight : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] float hoverScale = 1.1f;
    [SerializeField] float pulseSpeed = 3f;
    [SerializeField] float pulseAmount = 0.05f;
    [SerializeField] float outlineMin = 3f;
    [SerializeField] float outlineMax = 6f;
    [SerializeField] float outlinePressed = 9f;

    Vector3 baseScale;
    bool hovering, pressed;
    Material mat;

    void Awake()
    {
        baseScale = transform.localScale;

        var img = GetComponent<Image>();
        mat = new Material(img.material);   // per-object copy of PanOutlineMat
        img.material = mat;
    }

    void OnDestroy()
    {
        if (mat) Destroy(mat);
    }

    void Update()
    {
        float target = pressed ? 0.9f : (hovering ? hoverScale : 1f);
        float pulse = (!hovering && !pressed)
            ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount
            : 1f;

        transform.localScale = Vector3.Lerp(
            transform.localScale, baseScale * target * pulse, Time.deltaTime * 12f);

        float size = pressed
            ? outlinePressed
            : Mathf.Lerp(outlineMin, outlineMax, Mathf.PingPong(Time.time * 1.5f, 1f));
        mat.SetFloat("_OutlineSize", size);
    }

    public void OnPointerEnter(PointerEventData e) { hovering = true; }
    public void OnPointerExit(PointerEventData e) { hovering = false; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
}