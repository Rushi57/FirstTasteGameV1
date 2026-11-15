using UnityEngine;

public class GameMechanicsFreezer : MonoBehaviour
{
    [Header("Optional: also shut these down (MainFridge, TopUtensils, CabinetMain)")]
    [SerializeField] private GameObject[] extraToStop;

    private CanvasGroup group;

    private void Awake()
    {
       
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
        group.interactable = false;
        group.blocksRaycasts = false;

     
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        foreach (var go in extraToStop)
            if (go != null) go.SetActive(false);

        SFXManager.Instance?.StopLoop(); 
    }
}