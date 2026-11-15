using UnityEngine;
using UnityEngine.UI;

public class GameOverPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button tapAnywhereButton;   
    [SerializeField] private GameObject gameOverBorder;  
    [SerializeField] private GameObject messageObject;   

    [Header("Settings")]
    [SerializeField] private float idleSeconds = 30f;

    private float idleTimer;
    private bool borderShown;

    private void Awake()
    {
        if (tapAnywhereButton != null)
            tapAnywhereButton.onClick.AddListener(ShowBorder);
    }


    private void OnEnable()
    {
        borderShown = false;
        idleTimer = 0f;

        if (gameOverBorder != null) gameOverBorder.SetActive(false);
        if (messageObject != null) messageObject.SetActive(true);
        if (tapAnywhereButton != null) tapAnywhereButton.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (borderShown) return;

        idleTimer += Time.unscaledDeltaTime;
        if (idleTimer >= idleSeconds)
            ShowBorder();
    }

    private void ShowBorder()
    {
        if (borderShown) return;
        borderShown = true;

        if (gameOverBorder != null) gameOverBorder.SetActive(true);
        if (messageObject != null) messageObject.SetActive(false);

        if (tapAnywhereButton != null) tapAnywhereButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (tapAnywhereButton != null)
            tapAnywhereButton.onClick.RemoveListener(ShowBorder);
    }
}