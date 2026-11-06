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

    // Runs every time ScoreManager calls gameOverPanel.SetActive(true)
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

        // unscaledDeltaTime so it still works if Time.timeScale = 0 on game over
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

        // Turn off the full-screen tap button so it doesn't block Replay/Quit
        if (tapAnywhereButton != null) tapAnywhereButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (tapAnywhereButton != null)
            tapAnywhereButton.onClick.RemoveListener(ShowBorder);
    }
}