using UnityEngine;
using UnityEngine.UI;

public class KnifeHolderToggle : MonoBehaviour
{
    [SerializeField] private GameObject knifeObject;
    [SerializeField] private bool startHiddent;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ToggleKnife);
    }

    private void Start()
    {
        if (knifeObject != null)
            knifeObject.SetActive(!startHiddent);
    }
    private void OnDestroy()
    {
        button.onClick.RemoveListener(ToggleKnife);
    }
    public void ToggleKnife()
    {
        if (knifeObject == null) return;
        knifeObject.SetActive(!knifeObject.activeSelf);
    }
}
