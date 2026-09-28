using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum StoveHeat { Off, Low, Medium, High }

/// <summary>
/// Put this on StoveDropZone (the object with the stove Image).
/// Tap: Off -> Low -> Medium -> High. Long press: Off.
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

    [Header("Cooking Prep")]
    [Tooltip("Step id completed when the stove reaches High. Must match the Cooking Step Ids entry in your RecipeData.")]
    public string highHeatStepId = "Stove:HighHeat";

    public StoveHeat CurrentHeat { get; private set; } = StoveHeat.Off;
    public event System.Action<StoveHeat> OnHeatChanged;

    private bool isHeld;
    private bool longPressFired;
    private float holdTimer;

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

    public void OnPointerDown(PointerEventData eventData)
    {
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

    public void SetHeat(StoveHeat newHeat)
    {
        if (newHeat == CurrentHeat) return;

        StoveHeat old = CurrentHeat;
        CurrentHeat = newHeat;

        Debug.Log($"[Stove] Heat changed: {old} -> {newHeat}");

        ApplyVisuals();
        OnHeatChanged?.Invoke(newHeat);

        if (newHeat == StoveHeat.High && !string.IsNullOrEmpty(highHeatStepId))
        {
            Debug.Log($"[Stove] High heat reached - completing prep step '{highHeatStepId}'");
            CookingPrepListUI.Instance?.CompleteStep(highHeatStepId);
        }
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
}