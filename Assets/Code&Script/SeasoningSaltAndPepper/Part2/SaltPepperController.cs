using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SaltPepperType { Salt, Pepper, CassavaStarch, Achuete, Sugar, PeanutButter, GlutinousRice, Safflower }

public class SaltPepperController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject saltPepperPanel;

    [Header("Step 1: Pick measuring tool (SeasoningPickPanel)")]
    [Tooltip("SeasoningPickPanel. Shown first whenever any ingredient is opened.")]
    public GameObject seasoningPickPanel;
    [Tooltip("Button on the 'Cup' card.")]
    public Button chooseCupButton;
    [Tooltip("Button on the 'Tbsp/Tsp' card.")]
    public Button chooseTbspTspButton;
    [Tooltip("Optional: close/cancel button of SeasoningPickPanel.")]
    public Button seasoningPickCloseButton;

    [Header("Step 2: Measurement panels (inside Salt/PepperPanel > Border)")]
    [Tooltip("PickCup (1, 1/2, 1/3, 1/4 cup)")]
    public GameObject pickCupPanel;
    [Tooltip("PickTbsp/Tps")]
    public GameObject pickTbspTspPanel;
    [Tooltip("Only needed if SeasoningPickPanel is a CHILD of Salt/PepperPanel: assign the 'Border' object " +
             "(jar + PickCup + PickTbsp/Tps). Leave empty if SeasoningPickPanel is a sibling of Salt/PepperPanel.")]
    public GameObject measurementBorder;

    [Header("Open buttons")]
    public Button saltButton;
    public Button pepperButton;
    public Button cassavaStarchButton;
    public Button achueteButton;
    public Button sugarButton;
    public Button peanutButterButton;
    public Button glutinousRiceButton;
    public Button safflowerButton;
    public Button closeButton;

    [Header("Jar visual")]
    public Image jarImage;
    public Sprite saltJarSprite;
    public Sprite pepperJarSprite;
    public Sprite achueteJarSprite;
    public Sprite sugarJarSprite;
    public Sprite peanutButterJarSprite;
    public Sprite cassavaStarchJarSprite;
    public Sprite glutinousRiceJarSprite;
    public Sprite safflowerJarSprite;

    [Header("Stove-top spoon animation")]
    [Tooltip("PourAnimationController on TbpsSalt&PepperImageSeasoning. Leave its Liquid Image EMPTY (no pour).")]
    public PourAnimationController spoonAnimation;

    private Sprite pendingSpoonSprite;

    [Tooltip("PourAnimationController on the teaspoon animation object under AnimationGameObject.")]
    public PourAnimationController tspAnimation;
    public Color saltColor = Color.white;
    public Color pepperColor = Color.white;
    public Color cassavaStarchColor = Color.white;
    public Color achueteColor = Color.white;
    public Color sugarColor = Color.white;
    public Color peanutButterColor = Color.white;
    public Color glutinousRiceColor = Color.white;
    public Color safflowerColor = new Color(0.91f, 0.64f, 0.09f); // golden-orange

    [Header("Recipe check")]
    public CookingPrepPanelController cookingPrepPanel;
    public bool penalizeWrongAmount = true;

    private SaltPepperType currentType;
    private bool pendingSuccess;
    private int pendingLineIndex = -1;

    [Header("Wrong drop penalty")]
    public int wrongDropPoints = 5;
    public int wrongDropHearts = 1;

    [Header("Pan liquid")]
    public PanLiquidFill panLiquid;
    public float mlPerTsp = 5f;   // 1 cup = 48 tsp = 240 ml
    private float pendingTsp;

    [Header("Value popup (MessageBorder)")]
    [Tooltip("The MessageBorder object. Leave it inactive in the scene.")]
    public GameObject valueBorder;
    [Tooltip("The TMP text inside MessageBorder.")]
    public TMP_Text valueBorderText;
    public float valueBorderDuration = 1.2f;

    [Header("Wrong-pick feedback")]
    [Tooltip("The panel that shows an error message (e.g. MechanicMessagePanel).")]
    public GameObject wrongMessagePanel;
    [Tooltip("The TMP text inside it (e.g. MessageIfWrongText).")]
    public TMP_Text wrongMessageText;
    [Tooltip("How long the message stays visible before auto-hiding.")]
    public float wrongMessageDuration = 1.5f;

    private Coroutine valueBorderRoutine;
    private Coroutine wrongMessageRoutine;

    private void Start()
    {
        // Nested layout (SeasoningPickPanel is a child of Salt/PepperPanel): if Measurement Border
        // was left empty, find it automatically = the child of Salt/PepperPanel that holds PickCup/PickTbsp.
        if (measurementBorder != null && measurementBorder == seasoningPickPanel)
        {
            Debug.LogWarning("[SaltPepper] Measurement Border is set to SeasoningPickPanel - it must be the 'Border' object. Auto-detecting instead.");
            measurementBorder = null;
        }

        if (measurementBorder == null && saltPepperPanel != null && seasoningPickPanel != null
            && seasoningPickPanel.transform.IsChildOf(saltPepperPanel.transform))
        {
            Transform t = pickCupPanel != null ? pickCupPanel.transform
                        : pickTbspTspPanel != null ? pickTbspTspPanel.transform : null;
            while (t != null && t.parent != saltPepperPanel.transform) t = t.parent;
            if (t != null && t.gameObject != seasoningPickPanel)
            {
                measurementBorder = t.gameObject;
                Debug.Log($"[SaltPepper] Auto-detected Measurement Border: {t.name}");
            }
        }

        if (saltButton != null) saltButton.onClick.AddListener(() => OpenFor(SaltPepperType.Salt));
        if (pepperButton != null) pepperButton.onClick.AddListener(() => OpenFor(SaltPepperType.Pepper));
        if (cassavaStarchButton != null) cassavaStarchButton.onClick.AddListener(() => OpenFor(SaltPepperType.CassavaStarch));
        if (achueteButton != null) achueteButton.onClick.AddListener(() => OpenFor(SaltPepperType.Achuete));
        if (sugarButton != null) sugarButton.onClick.AddListener(() => OpenFor(SaltPepperType.Sugar));
        if (peanutButterButton != null) peanutButterButton.onClick.AddListener(() => OpenFor(SaltPepperType.PeanutButter));
        if (glutinousRiceButton != null) glutinousRiceButton.onClick.AddListener(() => OpenFor(SaltPepperType.GlutinousRice));
        if (safflowerButton != null) safflowerButton.onClick.AddListener(() => OpenFor(SaltPepperType.Safflower));

        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);

        if (chooseCupButton != null) chooseCupButton.onClick.AddListener(ChooseCup);
        if (chooseTbspTspButton != null) chooseTbspTspButton.onClick.AddListener(ChooseTbspTsp);
        if (seasoningPickCloseButton != null) seasoningPickCloseButton.onClick.AddListener(CancelMeasureChoice);

        if (valueBorder != null) valueBorder.SetActive(false);
        if (seasoningPickPanel != null) seasoningPickPanel.SetActive(false);
        if (pickCupPanel != null) pickCupPanel.SetActive(false);
        if (pickTbspTspPanel != null) pickTbspTspPanel.SetActive(false);

        spoonAnimation?.Hide();
        tspAnimation?.Hide();
    }

    // ------------------------------------------------------------------
    // Flow: ingredient button -> SeasoningPickPanel -> (Cup | Tbsp/Tsp) -> drag onto jar
    // ------------------------------------------------------------------

    public void OpenFor(SaltPepperType type)
    {
        var prep = CookingPrepListUI.Instance;

        if (prep == null)
        {
            ScoreManager.Instance?.ReportMistake("Gather all ingredients first!", 0, 0);
            return;
        }

        string current = prep.CurrentStepId;
        string prefix = $"{type}:";
        if (current == null || !current.StartsWith(prefix, StringComparison.Ordinal))
        {
            Debug.Log($"[SaltPepper] Blocked {type}. Current step is '{current}'.");
            ScoreManager.Instance?.ReportMistake("Wrong Step!\nMinus 1 heart", 0, 1);
            return;
        }

        currentType = type;
        pendingSuccess = false;
        pendingLineIndex = -1;
        if (jarImage != null) jarImage.sprite = GetJarSprite(type);
        if (valueBorder != null) valueBorder.SetActive(false);

        // Show the "Cup or Tbsp/Tsp?" choice first.
        if (measurementBorder != null)
        {
            // SeasoningPickPanel is nested inside Salt/PepperPanel: the parent must stay active.
            saltPepperPanel.SetActive(true);
            measurementBorder.SetActive(false);
        }
        else
        {
            saltPepperPanel.SetActive(false);
        }
        if (seasoningPickPanel != null)
        {
            seasoningPickPanel.SetActive(true);
            Debug.Log($"[SaltPepper] {type}: waiting for measuring tool choice.");
        }
        else
        {
            // No choice panel assigned: fall back to the old tsp/tbsp behaviour.
            ChooseTbspTsp();
        }
    }

    public void ChooseCup() => OpenMeasurementPanel(useCup: true);
    public void ChooseTbspTsp() => OpenMeasurementPanel(useCup: false);

    private void OpenMeasurementPanel(bool useCup)
    {
        saltPepperPanel.SetActive(true);
        if (measurementBorder != null) measurementBorder.SetActive(true);
        if (pickCupPanel != null) pickCupPanel.SetActive(useCup);
        if (pickTbspTspPanel != null) pickTbspTspPanel.SetActive(!useCup);
        if (valueBorder != null) valueBorder.SetActive(false);

        // Hide the choice panel last so nothing above can turn it back on.
        if (seasoningPickPanel != null) seasoningPickPanel.SetActive(false);

        Debug.Log($"[SaltPepper] Opened {currentType} with {(useCup ? "CUP" : "TBSP/TSP")}. Waiting for a drop.");
    }

    public void CancelMeasureChoice()
    {
        if (seasoningPickPanel != null) seasoningPickPanel.SetActive(false);
        saltPepperPanel.SetActive(false);
    }

    public void RequestClose()
    {
        saltPepperPanel.SetActive(false);
        if (seasoningPickPanel != null) seasoningPickPanel.SetActive(false);

        if (pendingSuccess)
        {
            string stepId = $"{currentType}:{PendingId}";
            Color color = GetColor(currentType);
            float tspToAdd = pendingTsp;
            SaltPepperType typeAtClose = currentType;

            Action onDone = () =>
            {
                CookingPrepListUI.Instance?.CompleteStep(stepId);

                switch (typeAtClose)
                {
                    case SaltPepperType.CassavaStarch:
                        panLiquid?.AddLiquidById("CassavaStarch", tspToAdd * mlPerTsp);
                        break;
                    case SaltPepperType.Achuete:
                        panLiquid?.AddLiquidById("Achuete", tspToAdd * mlPerTsp);
                        break;
                    case SaltPepperType.Sugar:
                        panLiquid?.AddLiquidById("Sugar", tspToAdd * mlPerTsp);
                        break;
                    case SaltPepperType.PeanutButter:
                        panLiquid?.AddLiquidById("PeanutButter", tspToAdd * mlPerTsp);
                        break;
                    case SaltPepperType.GlutinousRice:
                        panLiquid?.AddLiquidById("GlutinousRice", tspToAdd * mlPerTsp);
                        break;
                    case SaltPepperType.Safflower:
                        panLiquid?.AddLiquidById("Safflower", tspToAdd * mlPerTsp);
                        break;
                }
            };

            if (spoonAnimation != null)
            {
                spoonAnimation.SetSprite(pendingSpoonSprite);
                spoonAnimation.PlayPourAnimation(color, onDone);
            }
            else onDone();
        }

        pendingSuccess = false;
        pendingLineIndex = -1;
        Debug.Log("[SaltPepper] Closed.");
    }

    private string PendingId;

    public void HandleSpoonDropped(SaltPepperSpoonItem spoon, TestDrag drag, GameObject dropped)
    {
        ShowValuePopup($"{spoon.Label} of {GetDisplayName(currentType)}");
        RecipeData recipe = cookingPrepPanel != null ? cookingPrepPanel.currentRecipe : null;

        if (recipe == null) return;

        string keyword = GetKeyword(currentType);

        if (!TryFindRequirement(recipe, keyword, out int lineIndex, out float requiredTsp))
        {
            string msg = $"No {GetDisplayName(currentType)} needed right now!";
            if (penalizeWrongAmount)
                ScoreManager.Instance?.ReportMistake(msg, wrongDropPoints, wrongDropHearts);
            else
                ShowWrongMessage(msg);
            return;
        }

        bool correct = Mathf.Abs(spoon.TspValue - requiredTsp) <= 0.05f;
        pendingTsp = requiredTsp;

        if (correct)
        {
            ScoreManager.Instance?.ReportResult(ResultQuality.VeryGood);

            pendingSuccess = true;

            Image spoonImg = dropped.GetComponent<Image>();
            if (spoonImg == null) spoonImg = dropped.GetComponentInChildren<Image>();
            pendingSpoonSprite = spoonImg != null ? spoonImg.sprite : null;

            pendingLineIndex = lineIndex;
            PendingId = spoon.measurement.ToString();

            drag.SnapTo(spoon.HomeParent);
            dropped.transform.localRotation = Quaternion.identity;
            dropped.transform.localScale = Vector3.one;
        }
        else
        {
            string msg = $"Wrong amount! Recipe needs {FormatAmount(requiredTsp)} of {GetDisplayName(currentType)}.";

            if (penalizeWrongAmount)
                ScoreManager.Instance?.ReportMistake(msg, wrongDropPoints, wrongDropHearts);
            else
                ShowWrongMessage(msg);
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

    // ------------------------------------------------------------------
    // Amount formatting / parsing (cups are stored internally as teaspoons)
    // ------------------------------------------------------------------

    private static string FormatAmount(float tsp)
    {
        const float cup = SpoonMeasurementTable.TspPerCup;

        if (tsp >= cup / 4f - 0.05f)   // 1/4 cup or more -> show in cups
        {
            if (Mathf.Abs(tsp - cup) < 0.05f) return "1 cup";
            if (Mathf.Abs(tsp - cup / 2f) < 0.05f) return "1/2 cup";
            if (Mathf.Abs(tsp - cup / 3f) < 0.05f) return "1/3 cup";
            if (Mathf.Abs(tsp - cup / 4f) < 0.05f) return "1/4 cup";
            return $"{tsp / cup:0.##} cup";
        }
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

    // Now also understands "cup" / "cups"  (e.g. "add 1/3 cup glutinous rice")
    private static readonly Regex AmountRegex = new Regex(
        @"(\d+\s*/\s*\d+|\d+(?:\.\d+)?)\s*(?:of\s+)?(?:a\s+)?(tablespoons?|tbsp|tbps|teaspoons?|tsp|cups?)\b",
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
        float unitTsp;
        if (unit.StartsWith("cup")) unitTsp = SpoonMeasurementTable.TspPerCup;
        else if (unit.StartsWith("tb") || unit.StartsWith("table")) unitTsp = 3f;
        else unitTsp = 1f;

        tsp = qty * unitTsp;
        return true;
    }

    private void ShowValuePopup(string message)
    {
        if (valueBorder == null) return;

        if (valueBorderText != null) valueBorderText.text = message;
        valueBorder.SetActive(true);

        if (valueBorderRoutine != null) StopCoroutine(valueBorderRoutine);
        valueBorderRoutine = StartCoroutine(HideValuePopupAfterDelay());
    }

    private IEnumerator HideValuePopupAfterDelay()
    {
        yield return new WaitForSeconds(valueBorderDuration);
        valueBorder.SetActive(false);
        valueBorderRoutine = null;
    }

    public void ResetProgress()
    {
        pendingSuccess = false;
        pendingLineIndex = -1;
        saltPepperPanel.SetActive(false);
        if (seasoningPickPanel != null) seasoningPickPanel.SetActive(false);
        spoonAnimation?.Hide();
        tspAnimation?.Hide();
        if (valueBorderRoutine != null) { StopCoroutine(valueBorderRoutine); valueBorderRoutine = null; }
    }

    private Sprite GetJarSprite(SaltPepperType t) => t switch
    {
        SaltPepperType.Salt => saltJarSprite,
        SaltPepperType.Pepper => pepperJarSprite,
        SaltPepperType.Achuete => achueteJarSprite,
        SaltPepperType.Sugar => sugarJarSprite,
        SaltPepperType.PeanutButter => peanutButterJarSprite,
        SaltPepperType.GlutinousRice => glutinousRiceJarSprite,
        SaltPepperType.Safflower => safflowerJarSprite,
        _ => cassavaStarchJarSprite
    };

    private Color GetColor(SaltPepperType t) => t switch
    {
        SaltPepperType.Salt => saltColor,
        SaltPepperType.Pepper => pepperColor,
        SaltPepperType.Achuete => achueteColor,
        SaltPepperType.Sugar => sugarColor,
        SaltPepperType.PeanutButter => peanutButterColor,
        SaltPepperType.GlutinousRice => glutinousRiceColor,
        SaltPepperType.Safflower => safflowerColor,
        _ => cassavaStarchColor
    };

    private static string GetKeyword(SaltPepperType t) => t switch
    {
        SaltPepperType.Salt => "salt",
        SaltPepperType.Pepper => "pepper",
        SaltPepperType.Achuete => "achuete",
        SaltPepperType.Sugar => "sugar",
        SaltPepperType.PeanutButter => "peanut",
        SaltPepperType.GlutinousRice => "glutinous",
        SaltPepperType.Safflower => "safflower",
        _ => "cassava"
    };

    private static string GetDisplayName(SaltPepperType t) => t switch
    {
        SaltPepperType.CassavaStarch => "Cassava Starch",
        SaltPepperType.PeanutButter => "Peanut Butter",
        SaltPepperType.GlutinousRice => "Glutinous Rice",
        SaltPepperType.Safflower => "Safflower",
        _ => t.ToString()
    };
}