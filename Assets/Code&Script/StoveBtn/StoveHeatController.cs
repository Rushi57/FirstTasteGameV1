using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum StoveHeat { Off, Low, Medium, High }

/// <summary>
/// Put this on StoveDropZone (the object with the stove Image).
/// Tap: Off -> Low -> Medium -> High. Long press: Off.
/// Reports stove steps to CookingPrepListUI in the order the recipe lists them.
/// </summary>
public class StoveHeatController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Input")]
    [Tooltip("Seconds the player must hold to turn the stove off.")]
    public float longPressTime = 0.6f;
    [Tooltip("If true, tapping while on High goes back to Low. If false, it stays on High.")]
    public bool wrapAroundAfterHigh = false;

    [Header("Visuals (all optional)")]
    public Image stoveImage;
    public Sprite offSprite;
    public Sprite lowSprite;
    public Sprite mediumSprite;
    public Sprite highSprite;
    public TMP_Text heatLabel;

    [Header("Recipe")]
    [Tooltip("The recipe being cooked (e.g. Adobo). Its stove instruction lines define the required heats, in order.")]
    public RecipeData recipe;

    [Tooltip("Heat must be held this long before the step counts, so tapping through Low -> Medium -> High doesn't complete a Medium step by accident. Set 0 for instant.")]
    public float confirmDelay = 1f;

    public StoveHeat CurrentHeat { get; private set; } = StoveHeat.Off;
    public event System.Action<StoveHeat> OnHeatChanged;

    private bool isHeld;
    private bool longPressFired;
    private float holdTimer;

    private int nextStoveStep = 0;
    private Coroutine confirmRoutine;

    private void Start()
    {
        ApplyVisuals();
        Debug.Log("[Stove] Ready. Heat = Off");
    }

    private void Update()
    {
        if (!isHeld || longPressFired) return;

        holdTimer += Time.unscaledDeltaTime;
        if (holdTimer >= longPressTime)
        {
            longPressFired = true;
            Debug.Log($"[Stove] Long press detected ({holdTimer:0.00}s)");
            SetHeat(StoveHeat.Off);
        }
    }

    // ---------------- Input ----------------

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[Stove] PointerDown received");
        isHeld = true;
        longPressFired = false;
        holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHeld && !longPressFired)
            HandleTap();

        isHeld = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Finger slid off the stove - cancel, no tap or long press
        isHeld = false;
    }

    private void HandleTap()
    {
        Debug.Log($"[Stove] Tap detected (current heat: {CurrentHeat})");

        switch (CurrentHeat)
        {
            case StoveHeat.Off: SetHeat(StoveHeat.Low); break;
            case StoveHeat.Low: SetHeat(StoveHeat.Medium); break;
            case StoveHeat.Medium: SetHeat(StoveHeat.High); break;
            case StoveHeat.High:
                if (wrapAroundAfterHigh) SetHeat(StoveHeat.Low);
                else Debug.Log("[Stove] Already on High - long press to turn off.");
                break;
        }
    }

    // ---------------- Heat ----------------

    public void SetHeat(StoveHeat newHeat)
    {
        if (newHeat == CurrentHeat) return;

        StoveHeat old = CurrentHeat;
        CurrentHeat = newHeat;
        Debug.Log($"[Stove] Heat changed: {old} -> {newHeat}");

        ApplyVisuals();
        OnHeatChanged?.Invoke(newHeat);

        // Cancel any pending check from the previous heat
        if (confirmRoutine != null)
        {
            StopCoroutine(confirmRoutine);
            confirmRoutine = null;
        }

        if (newHeat != StoveHeat.Off && isActiveAndEnabled)
            confirmRoutine = StartCoroutine(ConfirmHeatRoutine(newHeat));
    }

    private System.Collections.IEnumerator ConfirmHeatRoutine(StoveHeat heat)
    {
        if (confirmDelay > 0f)
            yield return new WaitForSeconds(confirmDelay);
        confirmRoutine = null;

        if (recipe == null)
        {
            Debug.LogWarning("[Stove] No recipe assigned - can't check required heat.");
            yield break;
        }

        // Find the stove step whose order is the next expected one
        for (int i = 0; i < recipe.cookingInstructions.Count; i++)
        {
            if (!recipe.TryGetStoveStep(i, out int order, out StoveHeat required)) continue;
            if (order != nextStoveStep) continue;

            if (required == heat)
            {
                string id = RecipeData.StoveStepId(order, required);
                Debug.Log($"[Stove] Step {order} done: {required} held for {confirmDelay}s -> '{id}'");
                CookingPrepListUI.Instance?.CompleteStep(id);
                nextStoveStep++;
            }
            else
            {
                Debug.Log($"[Stove] Waiting for {required} (step {order}), but stove is {heat}");
            }
            yield break;
        }

        Debug.Log("[Stove] No more stove steps in this recipe.");
    }

    /// <summary>Call on Retry / new dish, next to CookingPrepListUI.ResetProgress().</summary>
    public void ResetStoveProgress()
    {
        nextStoveStep = 0;
        SetHeat(StoveHeat.Off);
    }

    private void ApplyVisuals()
    {
        if (heatLabel != null) heatLabel.text = CurrentHeat.ToString();

        if (stoveImage == null) return;

        Sprite s = CurrentHeat switch
        {
            StoveHeat.Low => lowSprite,
            StoveHeat.Medium => mediumSprite,
            StoveHeat.High => highSprite,
            _ => offSprite
        };
        if (s != null) stoveImage.sprite = s;
    }
    public void SetRecipe(RecipeData newRecipe)
    {
        recipe = newRecipe;
        ResetStoveProgress();
        Debug.Log($"[Stove] Recipe set to '{(newRecipe != null ? newRecipe.recipeName : "null")}'");
    }
}