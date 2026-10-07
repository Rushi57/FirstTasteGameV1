using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PanDropZone : MonoBehaviour, IDropHandler
{
    [Header("Requirements")]
    public bool requireHeat = true;

    [Header("On accept")]
    public bool consumeIngredient = false;

    [Header("Placement")]
    [Tooltip("Where accepted ingredients snap to. Leave empty to use this object's own RectTransform.")]
    public RectTransform ingredientAnchor;

    [Header("Spatula (mixing)")]
    public string spatulaId = "Spatula";
    public MixingSeasoningController mixingController;

    [Header("Lid(boil/simmer)")]
    public string lidId = "PanLid";
    public BoilSimmerController boilSimmerController;

    [Header("Pour Animation")]
    [Tooltip("AnimationDropGameObject - the object with PourAnimationPlayer.")]
    public PourAnimationPlayer pourPlayer;

    [Header("Stove")]
    public StoveHeatController stoveController;

    public event System.Action<IngredientData> OnIngredientAdded;

    private TutorialInteractable tutorialTag;
    private static bool InTutorial =>
    TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
    private StoveHeatController ResolveStove() =>
    stoveController != null ? stoveController : GetComponentInParent<StoveHeatController>();
    private void Awake()
    {
        tutorialTag = GetComponent<TutorialInteractable>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null || dropped == gameObject) return;

        TestDrag drag = dropped.GetComponent<TestDrag>();
        if (drag == null || drag.IsLocked) return;

        // Spatula: opens the mixing mini-game instead of being added as an ingredient
        if (drag.itemId == spatulaId)
        {
            if (!HeatOk("Spatula")) return;
          
            Debug.Log("[PanDrop] Spatula dropped on pan - opening mixing mini-game.");
            mixingController?.OpenAndConfigure();
            tutorialTag?.ReportDrop();
            return; // spatula bounces back to its rack via TestDrag
        }
        //PanLid
        if(drag.itemId == lidId)
        {
            if (!HeatOk("Lid")) return;
            Debug.Log("[PanDrop] Lid dropped on pan - opening Boil/Simmer mini-game.");
            if (CookingPrepListUI.Instance != null && !CookingPrepListUI.Instance.TryAccept("Pan:PanLid"))
                return;
            CookingPrepListUI.Instance?.CompleteStep("Pan:PanLid");
            boilSimmerController?.OpenAndConfigure();
            tutorialTag?.ReportDrop();
            return; // lid bounces back to its spot via TestDrag
        }
        var interactable = dropped.GetComponent<TutorialInteractable>();
        IngredientData data = interactable != null ? interactable.sourceData as IngredientData : null;

        if (data == null)
        {
            Debug.LogWarning($"[PanDrop] {dropped.name} has no IngredientData (TutorialInteractable.sourceData) - ignoring.");
            return;
        }

        if (!HeatOk(data.id)) return;

      

        string stepId = $"Pan:{data.id}";

        if (CookingPrepListUI.Instance != null && !CookingPrepListUI.Instance.TryAccept(stepId))
            return;

        // ---------- Pour items (e.g. the water pitcher) ----------
        if (data.playsPourAnimation)
        {
            HandlePour(drag, data, stepId);
            return;
        }

        // ---------- Normal ingredients ----------
        Debug.Log($"[PanDrop] Added '{data.id}' to the pan -> completing '{stepId}'");

        CookingPrepListUI.Instance?.CompleteStep(stepId);
        OnIngredientAdded?.Invoke(data);

        RectTransform target = ingredientAnchor != null ? ingredientAnchor : transform as RectTransform;

        if (consumeIngredient)
        {
            dropped.transform.SetParent(target, false);
            dropped.SetActive(false);
        }
        else
        {
            drag.SnapTo(target);
        }
        tutorialTag?.ReportDrop();
    }

    private void HandlePour(TestDrag drag, IngredientData data, string stepId)
    {
        if (pourPlayer == null)
        {
            Debug.LogWarning("[PanDrop] Pour Player is not assigned on PanDropZone.");
            return; // item returns normally via TestDrag.OnEndDrag
        }

        bool started = pourPlayer.Play(() =>
        {
            // Pour finished: the animated pitcher is already hidden by PourAnimationPlayer.
            // Bring the table pitcher back...
            if (drag != null) drag.SetLocked(false);

            // ...and gray out "Add 1 cup of water" in the Cooking Prep list.
            Debug.Log($"[PanDrop] Pour finished -> completing '{stepId}'");

            CookingPrepListUI.Instance?.CompleteStep(stepId);
            OnIngredientAdded?.Invoke(data);
            tutorialTag?.ReportDrop();
        });

        if (started)
        {
            // Send the table pitcher back to its slot and hide it while the animation plays.
            drag.LockAndReturn();
        }
        // If the animation was already busy, the drop is ignored and the item just returns, still visible.
    }

    /// <summary>True if heat isn't required, or the stove is on.</summary>
    private bool HeatOk(string what)
    {
        if (!requireHeat) return true;

        StoveHeatController s = ResolveStove();
        if (s != null && s.CurrentHeat != StoveHeat.Off) return true;

        Debug.Log($"[PanDrop] '{what}' dropped but the stove isn't heated (stove={(s != null ? s.CurrentHeat.ToString() : "NULL")}) - rejecting.");

        // No hearts/points lost while the tutorial is teaching
        if (!InTutorial)
            ScoreManager.Instance?.ReportResult(ResultQuality.Bad);

        return false;
    }

}