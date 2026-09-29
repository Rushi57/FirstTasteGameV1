using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SaltPepperType { Salt, Pepper }

/// <summary>
/// Flow: tap Salt/Pepper -> Salt/PepperPanel opens, jar shows salt or pepper
///       -> player drags a spoon (Tbsp/tsp) onto the jar -> scored against
///       the recipe -> Close -> if correct, plays the stove-top spoon
///       animation, then grays out the Cooking Prep row.
/// </summary>
public class SaltPepperController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject saltPepperPanel;   // Salt/PepperPanel

    [Header("Open buttons")]
    public Button saltButton;
    public Button pepperButton;
    public Button closeButton;

    [Header("Jar visual")]
    public Image jarImage;
    public Sprite saltJarSprite;
    public Sprite pepperJarSprite;

    [Header("Stove-top spoon animation")]
    [Tooltip("PourAnimationController on the tablespoon animation object under AnimationGameObject.")]
    public PourAnimationController tbspAnimation;
    [Tooltip("PourAnimationController on the teaspoon animation object under AnimationGameObject.")]
    public PourAnimationController tspAnimation;
    public Color saltColor = Color.white;
    public Color pepperColor = new Color(0.15f, 0.1f, 0.05f, 1f);

    [Header("Recipe check")]
    public CookingPrepPanelController cookingPrepPanel;
    public bool penalizeWrongAmount = true;

    private SaltPepperType currentType;
    private bool pendingSuccess;
    private int pendingLineIndex = -1;
    private bool pendingIsTbsp;

    [Header("Wrong-pick feedback")]
    [Tooltip("The panel that shows an error message (e.g. MechanicMessagePanel).")]
    public GameObject wrongMessagePanel;
    [Tooltip("The TMP text inside it (e.g. MessageIfWrongText).")]
    public TMP_Text wrongMessageText;
    [Tooltip("How long the message stays visible before auto-hiding.")]
    public float wrongMessageDuration = 1.5f;

    private Coroutine wrongMessageRoutine;

    private void Start()
    {
        if (saltButton != null) saltButton.onClick.AddListener(() => OpenFor(SaltPepperType.Salt));
        if (pepperButton != null) pepperButton.onClick.AddListener(() => OpenFor(SaltPepperType.Pepper));
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);

        tbspAnimation?.Hide();
        tspAnimation?.Hide();
    }

    public void OpenFor(SaltPepperType type)
    {
        currentType = type;
        pendingSuccess = false;
        pendingLineIndex = -1;

        if (jarImage != null)
            jarImage.sprite = type == SaltPepperType.Salt ? saltJarSprite : pepperJarSprite;

        saltPepperPanel.SetActive(true);
        Debug.Log($"[SaltPepper] Opened for {type}. Waiting for a spoon drop.");
    }

    public void RequestClose()
    {
        saltPepperPanel.SetActive(false);

        if (pendingSuccess)
        {
            PourAnimationController anim = pendingIsTbsp ? tbspAnimation : tspAnimation;
            string stepId = $"{currentType}:{PendingId}";
            int lineIndex = pendingLineIndex;
            Color color = currentType == SaltPepperType.Salt ? saltColor : pepperColor;

            if (anim != null)
            {
                Debug.Log($"[SaltPepper] Closed after a correct drop - playing {(pendingIsTbsp ? "Tbsp" : "Tsp")} animation before completing '{stepId}'.");
                anim.PlayPourAnimation(color, () =>
                {
                    CookingPrepListUI.Instance?.CompleteStep(stepId);
                    Debug.Log($"[SaltPepper] '{stepId}' completed after animation.");
                });
            }
            else
            {
                Debug.LogWarning("[SaltPepper] No animation assigned - completing step without animation.");
                CookingPrepListUI.Instance?.CompleteStep(stepId);
            }
        }

        pendingSuccess = false;
        pendingLineIndex = -1;
        Debug.Log("[SaltPepper] Closed.");
    }

    private string PendingId; // set right before RequestClose reads it

    public void HandleSpoonDropped(SaltPepperSpoonItem spoon, TestDrag drag, GameObject dropped)
    {
        RecipeData recipe = cookingPrepPanel != null ? cookingPrepPanel.currentRecipe : null;
        if (recipe == null)
        {
            Debug.LogWarning("[SaltPepper] No recipe available (assign Cooking Prep Panel).");
            return;
        }

        string keyword = currentType == SaltPepperType.Salt ? "salt" : "pepper";

        if (!TryFindRequirement(recipe, keyword, out int lineIndex, out float requiredTsp))
        {
            Debug.Log($"[SaltPepper] Recipe has no (remaining) '{currentType}' step - nothing to score. Bouncing spoon back.");
            ShowWrongMessage($"No {currentType} needed right now!");
            if (penalizeWrongAmount) ScoreManager.Instance?.ReportResult(ResultQuality.Bad);
            return;
        }

        bool correct = Mathf.Abs(spoon.TspValue - requiredTsp) <= 0.05f;

        Debug.Log($"[SaltPepper] {currentType}: dropped {spoon.Label} ({spoon.TspValue} tsp), recipe wants {requiredTsp:0.###} tsp -> {(correct ? "CORRECT" : "WRONG")}");

        if (correct)
        {
            ScoreManager.Instance?.ReportResult(ResultQuality.VeryGood);

            pendingSuccess = true;
            pendingLineIndex = lineIndex;
            pendingIsTbsp = spoon.measurement == SpoonMeasurement.Tbsp_1 || spoon.measurement == SpoonMeasurement.Tbsp_1_2;
            PendingId = spoon.measurement.ToString();

            drag.SnapTo(spoon.HomeParent);
            dropped.transform.localRotation = Quaternion.identity;
            dropped.transform.localScale = Vector3.one;
        }
        else
        {
            ShowWrongMessage($"Wrong amount! Recipe needs {FormatTsp(requiredTsp)} of {currentType}.");

            if (penalizeWrongAmount)
                ScoreManager.Instance?.ReportResult(ResultQuality.Bad);
            // don't SnapTo - TestDrag bounces it back to its origin automatically
        }
    }

    private void ShowWrongMessage(string message)
    {
        if (wrongMessagePanel == null) return;

        if (wrongMessageText != null) wrongMessageText.text = message;
        wrongMessagePanel.SetActive(true);

        Debug.Log($"[SaltPepper] Wrong pick: {message}");

        if (wrongMessageRoutine != null) StopCoroutine(wrongMessageRoutine);
        wrongMessageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(wrongMessageDuration);
        wrongMessagePanel.SetActive(false);
        wrongMessageRoutine = null;
    }

    private static string FormatTsp(float tsp)
    {
        if (tsp >= 3f) return $"{tsp / 3f:0.##} tbsp";
        return $"{tsp:0.##} tsp";
    }

    private bool TryFindRequirement(RecipeData recipe, string keyword, out int lineIndex, out float tsp)
    {
        for (int i = 0; i < recipe.cookingInstructions.Count; i++)
        {
            string line = recipe.cookingInstructions[i];
            if (string.IsNullOrEmpty(line)) continue;

            string lower = line.ToLowerInvariant().TrimStart();
            if (!lower.StartsWith("add") || !lower.Contains(keyword)) continue;

            if (TryParseAmountTsp(lower, out tsp))
            {
                lineIndex = i;
                return true;
            }
        }
        lineIndex = -1;
        tsp = 0f;
        return false;
    }

    private static readonly Regex AmountRegex = new Regex(
        @"(\d+\s*/\s*\d+|\d+(?:\.\d+)?)\s*(?:of\s+)?(?:a\s+)?(tablespoons?|tbsp|tbps|teaspoons?|tsp)\b",
        RegexOptions.IgnoreCase);

    private static bool TryParseAmountTsp(string text, out float tsp)
    {
        tsp = 0f;
        Match m = AmountRegex.Match(text);
        if (!m.Success) return false;

        string q = m.Groups[1].Value.Replace(" ", "");
        float qty = q.Contains("/")
            ? float.Parse(q.Split('/')[0], CultureInfo.InvariantCulture) / float.Parse(q.Split('/')[1], CultureInfo.InvariantCulture)
            : float.Parse(q, CultureInfo.InvariantCulture);

        string unit = m.Groups[2].Value.ToLowerInvariant();
        float unitTsp = (unit.StartsWith("tb") || unit.StartsWith("table")) ? 3f : 1f;

        tsp = qty * unitTsp;
        return true;
    }

    public void ResetProgress()
    {
        pendingSuccess = false;
        pendingLineIndex = -1;
        saltPepperPanel.SetActive(false);
        tbspAnimation?.Hide();
        tspAnimation?.Hide();
    }
}