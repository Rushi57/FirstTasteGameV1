using UnityEngine;

public class GameMechanicsFreezer : MonoBehaviour
{
    [Header("Optional: also shut these down (MainFridge, TopUtensils, CabinetMain)")]
    [SerializeField] private GameObject[] extraToStop;

    private CanvasGroup group;

    private void Awake()
    {
        // Used to block touches/drags on anything still visible
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameOver += Freeze;
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameOver -= Freeze;
    }

    private void Freeze()
    {
        // Stop input
        group.interactable = false;
        group.blocksRaycasts = false;

        // Deactivating a panel stops its Update, coroutines and timers
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        foreach (var go in extraToStop)
            if (go != null) go.SetActive(false);

        SFXManager.Instance?.StopLoop();   // stops looping sounds like boiling
    }
}