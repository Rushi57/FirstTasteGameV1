using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class UIHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] float speed = 10f;

    Material mat;
    float target;
    float current;
    static readonly int ColorId = Shader.PropertyToID("_HighlightColor");
    static readonly int AmountId = Shader.PropertyToID("_HighlightAmount");

    void Awake()
    {
        var img = GetComponent<Image>();
        mat = new Material(img.material); // unique copy per button
        img.material = mat;
        mat.SetColor(ColorId, highlightColor);
        mat.SetFloat(AmountId, 0);
    }

    void Update()
    {
        current = Mathf.MoveTowards(current, target, speed * Time.unscaledDeltaTime);
        mat.SetFloat(AmountId, current);
    }

    public void OnPointerEnter(PointerEventData e) => target = 1;
    public void OnPointerExit(PointerEventData e) => target = 0;

    void OnDestroy() { if (mat) Destroy(mat); }
}