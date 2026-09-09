using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OSUCircleMechanic : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform targetCircle;
    [SerializeField] private RectTransform approachRing;
    [SerializeField] private Button targetButton;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private RectTransform spawnArea; // Drag Pepper/SaltPanel here

    [Header("Timing Settings")]
    [SerializeField] private float shrinkDuration = 1.2f;
    [SerializeField] private float perfectGraceWindow = 0.35f;

    [Header("Randomization Settings")]
    [SerializeField] private float minStartScale = 2.0f;
    [SerializeField] private float maxStartScale = 3.5f;
    [SerializeField] private int minNumber = 1;
    [SerializeField] private int maxNumber = 10;

    private float timer = 0f;
    private float currentStartScale = 2.5f;
    private bool isActive = false;

    private void Awake()
    {
        if (targetButton == null && targetCircle != null)
            targetButton = targetCircle.GetComponent<Button>();

        if (targetButton != null)
            targetButton.onClick.AddListener(OnTargetClicked);

        if (spawnArea == null)
            spawnArea = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        RandomizeAndReset();
    }

    public void RandomizeAndReset()
    {
        timer = 0f;
        isActive = true;

        // 1. Set random number text
        if (numberText != null)
        {
            numberText.text = Random.Range(minNumber, maxNumber + 1).ToString();
        }

        // 2. Set random approach ring starting scale
        currentStartScale = Random.Range(minStartScale, maxStartScale);
        if (approachRing != null)
        {
            approachRing.localScale = Vector3.one * currentStartScale;
        }

        // 3. Randomize Target Circle position within the Pepper/SaltPanel bounds
        if (spawnArea != null && targetCircle != null)
        {
            float widthBoundary = (spawnArea.rect.width - targetCircle.rect.width) / 2f;
            float heightBoundary = (spawnArea.rect.height - targetCircle.rect.height) / 2f;

            float randomX = Random.Range(-widthBoundary, widthBoundary);
            float randomY = Random.Range(-heightBoundary, heightBoundary);

            targetCircle.anchoredPosition = new Vector2(randomX, randomY);

            // Lock ApproachRing directly over TargetCircle
            approachRing.anchoredPosition = targetCircle.anchoredPosition;
        }
    }

    private void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / shrinkDuration);
        float scale = Mathf.Lerp(currentStartScale, 1.0f, progress);

        if (approachRing != null)
        {
            approachRing.localScale = Vector3.one * scale;
        }

        // MISS: Player did not tap in time
        if (timer >= shrinkDuration + perfectGraceWindow)
        {
            isActive = false;
            Debug.Log("MISS! (Too Late)");
            OnResultProcessed(0);
        }
    }

    private void OnTargetClicked()
    {
        if (!isActive) return;

        isActive = false;

        // PERFECT: Player tapped after ring fully covered target circle
        if (timer >= shrinkDuration && timer < shrinkDuration + perfectGraceWindow)
        {
            Debug.Log("PERFECT! (+300)");
            OnResultProcessed(300);
        }
        // GOOD: Player tapped while ring was still shrinking
        else if (timer < shrinkDuration)
        {
            Debug.Log("GOOD! (+100)");
            OnResultProcessed(100);
        }
    }

    private void OnResultProcessed(int score)
    {
        targetCircle.gameObject.SetActive(false);
        if (approachRing != null) approachRing.gameObject.SetActive(false);
    }
}