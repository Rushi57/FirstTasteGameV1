using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
    public event System.Action<IngredientData> OnIngredientRemoved;

    private TutorialInteractable tutorialTag;
    private static bool InTutorial =>
    TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
    private StoveHeatController ResolveStove() =>
    stoveController != null ? stoveController : GetComponentInParent<StoveHeatController>();

    [Header("Coocking")]
    public PanCookingEffect cookingEffect;
    [Header("Melting")]
    [Tooltip("Ingredient id that melts and vanishes when dropped in the pan.")]
    public string meltIngredientId = "Butter";

    

    [System.Serializable]
    public class PourLiquid
    {
        public string ingredientId;
        public PourAnimationPlayer player;   // leave empty to use the shared Pour Player
        public string liquidId;
        public float ml = 250f;

        [Header("Animation look")]
        public Sprite pitcherSprite;         // water glass, blood bowl, coconut milk pitcher...
        public Sprite streamSprite;          // use the WHITE stream sprite so the tint is exact
        public Color streamColor = Color.white;
    }




    [Header("Pan Liquid")]
    public PanLiquidFill panLiquid;
    public List<PourLiquid> pourLiquids = new List<PourLiquid>();

    [Header("Cooking Sound")]
    [Tooltip("The ingredient id of the cooking oil (must match IngredientData.id).")]
    public string oilId = "CookingOil";

    private enum PanSound { Frying, Boiling }
    private PanSound liquidSound = PanSound.Frying;
    private bool hasIngredient = false;

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
            var prep = CookingPrepListUI.Instance;

            // Spatula is only valid when a Mix step is the current one
            if (prep != null && prep.strictOrder && !prep.IsMixStepCurrent())
            {
                Debug.Log($"[PrepList] REJECTED spatula - current step is '{prep.CurrentStepId}'");
                ScoreManager.Instance?.ReportMistake(prep.wrongStepMessage);
                return;
            }

            if (!HeatOk("Spatula")) return;
            CabinetAutoCloser.Instance?.Close();

            SFXManager.Instance?.PlayPotLidClose();   // <-- SFX

            Debug.Log("[PanDrop] Spatula dropped on pan - opening mixing mini-game.");
            mixingController?.OpenAndConfigure();
            tutorialTag?.ReportDrop();
            return; // spatula bounces back to its rack via TestDrag
        }
        //PanLid
        if (drag.itemId == lidId)
        {
            if (!HeatOk("Lid")) return;

            var prep = CookingPrepListUI.Instance;
            string current = prep != null ? prep.CurrentStepId : null;

            // Simmer only when the list says the simmer step is next, otherwise treat as boil
            bool isSimmer = current == BoilSimmerController.SimmerStepId;
            string lidStepId = isSimmer ? BoilSimmerController.SimmerStepId
                                        : BoilSimmerController.BoilStepId;

            if (prep != null && !prep.TryAccept(lidStepId)) return;
            CabinetAutoCloser.Instance?.Close();
            SFXManager.Instance?.PlayPotLidClose();   // <-- added, after the step is accepted


            Debug.Log($"[PanDrop] Lid dropped on pan - step '{lidStepId}' - opening Boil/Simmer mini-game.");
            prep?.CompleteStep(lidStepId);
            boilSimmerController?.OpenAndConfigure(isSimmer ? CookMode.Simmer : CookMode.Boil);
            tutorialTag?.ReportDrop();
            return;
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

        // Only the butter melts; everything else browns as usual
        bool isMelt = data.id == meltIngredientId;

        if (!isMelt && cookingEffect != null)
        {
            Image img = dropped.GetComponent<Image>();
            if (img == null) img = dropped.GetComponentInChildren<Image>();
            cookingEffect.Track(img);
        }

        CookingPrepListUI.Instance?.CompleteStep(stepId);
        hasIngredient = true;
        UpdatePanSound();

        OnIngredientAdded?.Invoke(data);

        RectTransform target = ingredientAnchor != null ? ingredientAnchor : transform as RectTransform;

        if (isMelt)
        {
            drag.SnapTo(target);   // land on the pan first

            // Add the component automatically if it isn't on the object yet
            MeltAndVanish melt = dropped.GetComponent<MeltAndVanish>();
            if (melt == null) melt = dropped.AddComponent<MeltAndVanish>();
            melt.OnMeltFinished += () => OnIngredientRemoved?.Invoke(data);
            melt.BeginMelt();
        }
        else if (consumeIngredient)
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
        PourLiquid entry = pourLiquids.Find(p => p.ingredientId == data.id);
        PourAnimationPlayer player = entry != null && entry.player != null ? entry.player : pourPlayer;

        if (entry == null)
            Debug.LogWarning($"[PanDrop] No Pour Liquids entry for '{data.id}' - animation will play but the pan won't change.");

        if (player == null)
        {
            Debug.LogWarning($"[PanDrop] No pour player for '{data.id}' (add it to Pour Liquids or assign Pour Player).");
            return;
        }

        if (player.IsPlaying) return;

        if (entry != null)
            player.Configure(entry.pitcherSprite, entry.streamSprite, entry.streamColor);

        bool started = player.Play(() =>
        {
            if (drag != null) drag.SetLocked(false);

            if (panLiquid != null && entry != null)
                panLiquid.AddLiquidById(entry.liquidId, entry.ml);

            Debug.Log($"[PanDrop] Pour finished -> completing '{stepId}'");

            CookingPrepListUI.Instance?.CompleteStep(stepId);

            if (data.id == "Water") liquidSound = PanSound.Boiling;
            else if (data.id == oilId) liquidSound = PanSound.Frying;
            else hasIngredient = true;

            UpdatePanSound();

            OnIngredientAdded?.Invoke(data);
            tutorialTag?.ReportDrop();
        }, data);

        if (started) drag.LockAndReturn();
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
    public void NotifyIngredientRemoved(IngredientData data)
    {
        OnIngredientRemoved?.Invoke(data);
    }


    public void ResetCookingSound()
    {
        liquidSound = PanSound.Frying;
        hasIngredient = false;
        SFXManager.Instance?.StopLoop();
    }
    private void UpdatePanSound()
    {
        if (!hasIngredient) return;

        var sfx = SFXManager.Instance;
        if (sfx == null) return;

        StoveHeatController s = ResolveStove();
        if (s == null || s.CurrentHeat == StoveHeat.Off) return;

        AudioClip clip = liquidSound == PanSound.Boiling ? sfx.boilingWater : sfx.frying;
        if (clip == null) return;

        sfx.PlayLoop(clip);
    }
}