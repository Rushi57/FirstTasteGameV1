using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SeasoningType { Soy, Vinegar, CookingOil }
public enum MeasureTool { Spoon, Cup }

/// <summary>
/// Put this on an ALWAYS-ACTIVE object (e.g. GameMechanicsGameObject), NOT on
/// PourSeasoningPanel itself (it starts inactive, so its scripts wouldn't run).
///
/// Flow: tap Soy/Vinegar/Oil -> PourSeasoningPanel + PickPanel -> pick spoon or cup
///       -> PickPanel closes, PourImageBoarder opens with the matching scroll
///       -> pick a measurement -> hold PourButton -> release to grade
///       -> Close -> if the pour was correct, activates the matching stove-top
///          animation (CupImage or TbpsImage), tints it, plays Tilt -> Return,
///          and only then grays out the Cooking Prep row.
/// </summary>
public class PourSeasoningController : MonoBehaviour
{
    [Header("Tutorial")]
    [Tooltip("The TutorialInteractable on PourButton (id 'PourBtn').")]
    public TutorialInteractable pourTutorial;
    [Header("Panels")]
    public GameObject seasoningPanel;   // PourSeasoningPanel
    public GameObject pickPanel;        // PickPanel
    public GameObject pourBoard;        // PourImageBoarder
    public GameObject spoonScroll;      // TbspMesurementScrolling
    public GameObject cupScroll;        // CupMesurementPick

    [Header("Seasoning buttons (tap to open)")]
    public Button soyButton;
    public Button vinegarButton;
    public Button cookingOilButton;

    [Header("Tool pick buttons (inside PickPanel)")]
    public Button pickSpoonButton;
    public Button pickCupButton;

    [Header("Pour board")]
    public Button closeButton;
    [Tooltip("The PourMechanic on PourButton.")]
    public PourMechanic pourMechanic;

    [Header("Bottle / glass visual (PourImageBoarder)")]
    public Image glassImage;
    public Sprite soyGlassSprite;
    public Sprite vinegarGlassSprite;
    public Sprite cookingOilGlassSprite;

    [Header("Spoon fill (TableSpoon > FillSpoonImage)")]
    public Image fillSpoonImage;
    public Sprite soySpoonFillSprite;        // optional
    public Sprite vinegarSpoonFillSprite;    // optional
    public Sprite cookingOilSpoonFillSprite; // optional

    [Header("Measuring tool image (Tbps_Tsp_CupImage)")]
    [Tooltip("The Image on Tbps_Tsp_CupImage. Its sprite changes to the picked measurement.")]
    public Image toolImage;
    [Serializable]
    public class MeasurementSprite
    {
        public MeasureTool tool;
        [Tooltip("Must match the MeasurementButton's Ml value (e.g. 15 for 1 tbsp, 250 for 1 cup).")]
        public float ml;
        [Tooltip("Sprite shown on Tbps_Tsp_CupImage for this measurement.")]
        public Sprite toolSprite;
        [Tooltip("Optional: sprite for FillSpoonImage shaped to this measurement. Leave empty to keep the current one.")]
        public Sprite fillSprite;
        [Tooltip("Position of FillSpoonImage inside Tbps_Tsp_CupImage (its Pos X / Pos Y), so the liquid sits in the bowl.")]
        public Vector2 fillPosition;
        [Tooltip("Width / Height of FillSpoonImage for this measurement. Leave (0,0) to keep the current size.")]
        public Vector2 fillSize;
        [Tooltip("Optional: sprite used by the stove-top pour animation (TbpsImage / CupImage). Leave empty to use Tool Sprite.")]
        public Sprite animSprite;
    }
    [Tooltip("Tint the stove-top animation image with the seasoning color (multiplies the sprite).")]
    public bool tintAnimation = false;
    [Tooltip("One entry per measurement button (tool + ml -> sprite).")]
    public List<MeasurementSprite> measurementSprites = new List<MeasurementSprite>();
    [Tooltip("Resize the tool image to the sprite's native size (multiplied by this) when it changes. 0 = keep current size.")]
    public float nativeSizeMultiplier = 0f;

    [Header("Colors (particles + glass + stove animation)")]
    public Color soyColor = new Color(0.25f, 0.12f, 0.02f, 1f);      // dark brown
    public Color vinegarColor = new Color(0.85f, 0.85f, 0.6f, 1f);   // pale yellow
    public Color cookingOilColor = new Color(1f, 0.85f, 0.3f, 1f);   // golden yellow

    [Header("Stove-top pour animation")]
    [Tooltip("PourAnimationController on CupImage (under AnimationGameObject).")]
    public PourAnimationController cupAnimation;
    [Tooltip("PourAnimationController on TbpsImage (under AnimationGameObject).")]
    public PourAnimationController tbspAnimation;

    [Header("Optional UI")]
    public TMP_Text selectedLabel;

    [Header("Recipe check")]
    public CookingPrepPanelController cookingPrepPanel;
    [Range(0f, 1f)] public float matchTolerance = 0.03f;
    public bool penalizeWrongMeasurement = true;

    [Header("Pan Liquid")]
    public PanLiquidFill panLiquid;

    private float pendingMl;

    /// <summary>seasoning, chosen ml, was it fully correct (right amount + good pour).</summary>
    public event Action<SeasoningType, float, bool> OnPourConfirmed;

    private SeasoningType currentSeasoning;
    private MeasureTool chosenTool;
    private MeasurementButton selected;
    private readonly List<MeasurementButton> allButtons = new List<MeasurementButton>();
    private readonly HashSet<int> completedLines = new HashSet<int>();



    // Set once a correct pour lands; consumed when the panel is closed
    private MeasurementSprite pendingEntry;
    private bool pendingSuccess;
    private int pendingLineIndex = -1;

    private void Start()
    {
        if (soyButton != null) soyButton.onClick.AddListener(() => OpenFor(SeasoningType.Soy));
        if (vinegarButton != null) vinegarButton.onClick.AddListener(() => OpenFor(SeasoningType.Vinegar));
        if (cookingOilButton != null) cookingOilButton.onClick.AddListener(() => OpenFor(SeasoningType.CookingOil));

        if (pickSpoonButton != null) pickSpoonButton.onClick.AddListener(() => ChooseTool(MeasureTool.Spoon));
        if (pickCupButton != null) pickCupButton.onClick.AddListener(() => ChooseTool(MeasureTool.Cup));

        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);

        if (spoonScroll != null) allButtons.AddRange(spoonScroll.GetComponentsInChildren<MeasurementButton>(true));
        if (cupScroll != null) allButtons.AddRange(cupScroll.GetComponentsInChildren<MeasurementButton>(true));
        foreach (var b in allButtons) b.Clicked += OnMeasurementClicked;
        if (pourTutorial == null && pourMechanic != null)
            pourTutorial = pourMechanic.GetComponent<TutorialInteractable>();

        if (pourMechanic != null) pourMechanic.OnPourFinished += OnPourFinished;

        cupAnimation?.Hide();
        tbspAnimation?.Hide();

        Debug.Log($"[Pour] Ready. {allButtons.Count} measurement buttons found.");
    }

    private void OnDestroy()
    {
        if (pourMechanic != null) pourMechanic.OnPourFinished -= OnPourFinished;
        foreach (var b in allButtons)
            if (b != null) b.Clicked -= OnMeasurementClicked;
    }

    // ---------------- Panel flow ----------------

    public void OpenFor(SeasoningType seasoning)
    {
        //Wrong Step Gate
        string stepId = $"Pour:{seasoning}";
        var prep = CookingPrepListUI.Instance;
        if (prep != null && prep.CurrentStepId != stepId)
        {
            ScoreManager.Instance?.ReportMistake("Wrong Step!\nMinus 1 heart", 0, 1);
            return;
        }

        currentSeasoning = seasoning;
        ClearSelection();
        pendingSuccess = false;
        pendingLineIndex = -1;
        pendingMl = 0f;

        Color color = GetColor(seasoning);
        pourMechanic?.SetParticleColor(color);

        if (glassImage != null)
        {
            Sprite sprite = seasoning switch
            {
                SeasoningType.Soy => soyGlassSprite,
                SeasoningType.Vinegar => vinegarGlassSprite,
                _ => cookingOilGlassSprite
            };
            if (sprite != null) glassImage.sprite = sprite;
            glassImage.color = Color.white;
        }

        if (fillSpoonImage != null)
        {
            Sprite fillSprite = seasoning switch
            {
                SeasoningType.Soy => soySpoonFillSprite,
                SeasoningType.Vinegar => vinegarSpoonFillSprite,
                _ => cookingOilSpoonFillSprite
            };
            if (fillSprite != null) fillSpoonImage.sprite = fillSprite;

            Color c = color;
            c.a = fillSpoonImage.color.a;
            fillSpoonImage.color = c;
        }

        seasoningPanel.SetActive(true);
        pickPanel.SetActive(true);       // 1) player picks spoon or cup first
        pourBoard.SetActive(false);
        spoonScroll.SetActive(false);
        cupScroll.SetActive(false);

        Debug.Log($"[Pour] Opened for {seasoning}. Waiting for spoon/cup pick.");
    }

    public void ChooseTool(MeasureTool tool)
    {
        chosenTool = tool;

        pickPanel.SetActive(false);      // 2) close pick panel, open the pour board
        pourBoard.SetActive(true);
        spoonScroll.SetActive(tool == MeasureTool.Spoon);
        cupScroll.SetActive(tool == MeasureTool.Cup);

        // Meter scale = biggest measurement of the chosen tool
        GameObject scroll = tool == MeasureTool.Spoon ? spoonScroll : cupScroll;
        float max = 0f;
        foreach (var b in scroll.GetComponentsInChildren<MeasurementButton>(true))
            max = Mathf.Max(max, b.Ml);
        pourMechanic?.SetMeterMax(max);

        ClearSelection();
        Debug.Log($"[Pour] Tool chosen: {tool} (meter max {max:0.##} ml)");
    }

    /// <summary>Hooked to CloseBtn.</summary>
    public void RequestClose()
    {
        seasoningPanel.SetActive(false);

        if (pendingSuccess)
        {
            PourAnimationController anim = chosenTool == MeasureTool.Cup ? cupAnimation : tbspAnimation;
            string stepId = $"Pour:{currentSeasoning}";
            int lineIndex = pendingLineIndex;
            Color color = GetColor(currentSeasoning);
            SeasoningType seasoning = currentSeasoning;
            float ml = pendingMl;

            if (anim != null)
            {
                // Show the same spoon/cup the player picked in the stove-top animation
                if (pendingEntry != null)
                    anim.SetSprite(pendingEntry.animSprite != null ? pendingEntry.animSprite : pendingEntry.toolSprite);
                anim.SetColor(tintAnimation ? color : Color.white);

                Debug.Log($"[Pour] Panel closed after a correct pour - playing {chosenTool} animation before completing '{stepId}'.");

                anim.PlayPourAnimation(color, () =>
                {
                    completedLines.Add(lineIndex);
                    CookingPrepListUI.Instance?.CompleteStep(stepId);
                    panLiquid.AddLiquid(seasoning, color, ml);
                    if (panLiquid != null) panLiquid.AddLiquid(seasoning, color, ml);
                    Debug.Log($"[Pour] '{stepId}' completed after animation.");
                });
            }
            else
            {
                Debug.LogWarning($"[Pour] No animation assigned for {chosenTool} - completing step without animation.");
                completedLines.Add(lineIndex);
                CookingPrepListUI.Instance?.CompleteStep(stepId);
                panLiquid.AddLiquid(seasoning, color, ml);

            }
        }

        ClearSelection();
        pendingSuccess = false;
        pendingLineIndex = -1;
        pendingEntry = null;

        Debug.Log("[Pour] Closed.");
    }

    private Color GetColor(SeasoningType seasoning) => seasoning switch
    {
        SeasoningType.Soy => soyColor,
        SeasoningType.Vinegar => vinegarColor,
        _ => cookingOilColor
    };

    // ---------------- Selection ----------------

    private void OnMeasurementClicked(MeasurementButton btn)
    {
        selected = btn;
        foreach (var b in allButtons) b.SetSelected(b == btn);

        if (selectedLabel != null)
            selectedLabel.text = $"{btn.Label} ({btn.Ml:0.##} ml)";

        if (pourMechanic != null) pourMechanic.SetTarget(btn.Ml);

        ApplyToolVisual(btn.Ml);

        Debug.Log($"[Pour] Selected {btn.Label} = {btn.Ml:0.##} ml");
    }

    /// <summary>Swaps the cup/spoon sprite for the picked measurement and makes
    /// FillSpoonImage match its size. FillSpoonImage's color is left untouched.</summary>
    private MeasurementSprite FindEntry(float ml)
    {
        foreach (var e in measurementSprites)
            if (e.tool == chosenTool && Mathf.Abs(e.ml - ml) < 0.01f) return e;
        return null;
    }

    private void ApplyToolVisual(float ml)
    {
        if (toolImage == null) return;

        MeasurementSprite entry = FindEntry(ml);
        if (entry == null)
        {
            Debug.LogWarning($"[Pour] No sprite entry for {chosenTool} {ml:0.##} ml (add it to Measurement Sprites).");
            return;
        }

        if (entry.toolSprite != null)
        {
            toolImage.sprite = entry.toolSprite;
            toolImage.preserveAspect = true;
            if (nativeSizeMultiplier > 0f)
            {
                toolImage.SetNativeSize();
                RectTransform tr = toolImage.rectTransform;
                tr.sizeDelta = tr.sizeDelta * nativeSizeMultiplier;
            }
        }

        if (fillSpoonImage != null)
        {
            if (entry.fillSprite != null) fillSpoonImage.sprite = entry.fillSprite;

            // Place/size the liquid circle inside the bowl of this spoon/cup.
            RectTransform fr = fillSpoonImage.rectTransform;
            if (fr.parent == toolImage.rectTransform)
            {
                fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 0.5f);
                fr.pivot = new Vector2(0.5f, 0.5f);
                fr.anchoredPosition = entry.fillPosition;
                if (entry.fillSize.sqrMagnitude > 0f) fr.sizeDelta = entry.fillSize;
            }
        }
    }

    private void ClearSelection()
    {
        selected = null;
        foreach (var b in allButtons) b.SetSelected(false);
        if (selectedLabel != null) selectedLabel.text = "";
        if (pourMechanic != null) pourMechanic.ClearTarget();
    }

    // ---------------- Pour check ----------------

    private void OnPourFinished(PourMechanic.PourResult meter, float pouredMl)
    {

        if (selected == null) return;

        RecipeData recipe = cookingPrepPanel != null ? cookingPrepPanel.currentRecipe : null;
        if (recipe == null)
        {
            Debug.LogWarning("[Pour] No recipe available (assign Cooking Prep Panel).");
            return;
        }

        if (!TryFindRequirement(recipe, currentSeasoning, out int lineIndex, out float requiredMl))
        {
            Debug.Log($"[Pour] Recipe has no (remaining) '{currentSeasoning}' step - nothing to score.");
            return;
        }

        bool amountCorrect = Mathf.Abs(selected.Ml - requiredMl) <= Mathf.Max(0.05f, requiredMl * matchTolerance);
        bool meterOk = meter == PourMechanic.PourResult.Perfect || meter == PourMechanic.PourResult.Good;
        bool success = amountCorrect && meterOk;
        bool inTutorial = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
        ResultQuality quality = !success ? ResultQuality.Bad
                              : meter == PourMechanic.PourResult.Perfect ? ResultQuality.VeryGood
                              : ResultQuality.Good;

        Debug.Log($"[Pour] {currentSeasoning}: chose {selected.Ml:0.##} ml (recipe wants {requiredMl:0.##}), meter {meter} -> {quality}");

        if (!inTutorial && (penalizeWrongMeasurement || success))
            ScoreManager.Instance?.ReportResult(quality);

     

        if (success)
        {
            pendingSuccess = true;
            pendingLineIndex = lineIndex;
            pendingMl = selected.Ml;
            pendingEntry = FindEntry(selected.Ml);
        }

        OnPourConfirmed?.Invoke(currentSeasoning, selected.Ml, success);
        // Tell the tutorial LAST, once the result is known
        if (inTutorial)
        {
            if (success)
            {
                pourTutorial?.ReportHold();
            }
            else
            {
                ClearSelection();                 // resets the meter and unselects the measurement
                pourTutorial?.ReportMistake();    // revert to the previous step
            }
        }
    }

    private bool TryFindRequirement(RecipeData recipe, SeasoningType seasoning, out int lineIndex, out float ml)
    {
        string keyword = seasoning switch
        {
            SeasoningType.Soy => "soy",
            SeasoningType.Vinegar => "vinegar",
            _ => "oil"
        };

        for (int i = 0; i < recipe.cookingInstructions.Count; i++)
        {
            if (completedLines.Contains(i)) continue;

            string line = recipe.cookingInstructions[i];
            if (string.IsNullOrEmpty(line)) continue;

            string lower = line.ToLowerInvariant().TrimStart();
            if (!lower.StartsWith("add") || !lower.Contains(keyword)) continue;

            if (TryParseAmountMl(lower, out ml))
            {
                lineIndex = i;
                return true;
            }
        }

        lineIndex = -1;
        ml = 0f;
        return false;
    }

    private static readonly Regex AmountRegex = new Regex(
        @"(\d+\s*/\s*\d+|\d+(?:\.\d+)?)\s*(?:of\s+)?(?:a\s+)?(tablespoons?|tbsp|teaspoons?|tsp|cups?)\b",
        RegexOptions.IgnoreCase);

    private static bool TryParseAmountMl(string text, out float ml)
    {
        ml = 0f;
        Match m = AmountRegex.Match(text);
        if (!m.Success) return false;

        string q = m.Groups[1].Value.Replace(" ", "");
        float qty;
        if (q.Contains("/"))
        {
            string[] parts = q.Split('/');
            qty = float.Parse(parts[0], CultureInfo.InvariantCulture) / float.Parse(parts[1], CultureInfo.InvariantCulture);
        }
        else qty = float.Parse(q, CultureInfo.InvariantCulture);

        string unit = m.Groups[2].Value.ToLowerInvariant();
        float unitMl = unit.StartsWith("cup") ? 250f
                     : (unit.StartsWith("tb") || unit.StartsWith("table")) ? 15f
                     : 5f;

        ml = qty * unitMl;
        return true;
    }

    /// <summary>Call on Retry / new dish, next to the other reset calls.</summary>
    public void ResetProgress()
    {
        completedLines.Clear();
        pendingSuccess = false;
        pendingLineIndex = -1;
        seasoningPanel.SetActive(false);
        cupAnimation?.Hide();
        tbspAnimation?.Hide();
        panLiquid.Clear();
        ClearSelection();

    }


}