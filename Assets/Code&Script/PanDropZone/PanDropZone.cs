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

    [Header("Fill timing")]
    public float fillDelay = 0.6f;
    public float fillDuration = 1.5f;


    [System.Serializable]
    public class PourLiquid
    {
        public string ingredientId;
        public PourAnimationPlayer player;
        public string liquidId;
        public float ml = 250f;

        [Header("After pour")]
        [Tooltip("Destroy the dragged ingredient (e.g. Calamansi) when the pour finishes. It will NOT return to the table.")]
        public bool destroyIngredientAfterPour = false;
        [Tooltip("Also destroy the pour animation object when finished. Only tick this if the player is used just for this ingredient.")]
        public bool destroyAnimationAfterPour = false;

        [Header("Animation look")]
        public Sprite pitcherSprite;
        public Sprite streamSprite;
        public Color streamColor = Color.white;

        [Header("Fill timing")]
        public float fillDelay = 0.6f;
        public float fillDuration = 1.5f;
    }


    [Header("State change on drop")]
    [Tooltip("Matched against 'Target Name' in each ingredient's 'State Change On Drop' list.")]
    public string targetName = "Pan";

    [Header("Pan Liquid")]
    public PanLiquidFill panLiquid;
    public List<PourLiquid> pourLiquids = new List<PourLiquid>();

    [Header("Cooking Sound")]
    [Tooltip("The ingredient id of the cooking oil (must match IngredientData.id).")]
    public string oilId = "CookingOil";

    [Header("Wrong drop (no penalty)")]
    public string misdropMessage = "Wrong step!\nFollow the Cooking Prep list.";

  
    private void WarnNoPenalty(string message)
    {
      
        Debug.LogWarning("[PanDrop] " + message.Replace("\n", " "));
    }

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

       
        {
            RectTransform panRoot = ingredientAnchor != null ? ingredientAnchor : transform as RectTransform;
            var setAsidePrep = CookingPrepListUI.Instance;
            if (drag.OriginalParent == panRoot && setAsidePrep != null && setAsidePrep.IsSetAsideStepCurrent())
            {
                drag.ReturnToOrigin();
                drag.MarkDropHandled();   
                WarnNoPenalty(misdropMessage);
                return;
            }
        }


        if (drag.itemId == spatulaId)
        {
            var prep = CookingPrepListUI.Instance;


            if (prep != null && prep.strictOrder && !prep.IsMixStepCurrent())
            {
                ScoreManager.Instance?.ReportMistake(prep.wrongStepMessage);
                return;
            }

            if (!HeatOk("Spatula")) return;
            CabinetAutoCloser.Instance?.Close();
            SFXManager.Instance?.PlayPotLidClose();
            mixingController?.OpenAndConfigure();
            tutorialTag?.ReportDrop();
            return;
        }

        if (drag.itemId == lidId)
        {
            if (!HeatOk("Lid")) return;

            var prep = CookingPrepListUI.Instance;
            string current = prep != null ? prep.CurrentStepId : null;


            bool isSimmer = current == BoilSimmerController.SimmerStepId;
            string lidStepId = isSimmer ? BoilSimmerController.SimmerStepId
                                        : BoilSimmerController.BoilStepId;

            if (prep != null && !prep.TryAccept(lidStepId)) return;
            CabinetAutoCloser.Instance?.Close();
            SFXManager.Instance?.PlayPotLidClose();

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


        if (data.playsPourAnimation)
        {
            HandlePour(drag, data, stepId);
            return;
        }
 
        dropped.GetComponent<PlatedItem>()?.RemovePlate();

        Debug.Log($"[PanDrop] Added '{data.id}' to the pan -> completing '{stepId}'");


        bool isMelt = data.id == meltIngredientId;


        if (data.TryGetStateOnDrop(targetName, out var newState))
        {
            Image stateImg = dropped.GetComponent<Image>();
            if (stateImg == null) stateImg = dropped.GetComponentInChildren<Image>();
            if (stateImg != null)
                stateImg.sprite = data.GetSpriteForState(newState);
        }

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
            drag.SnapTo(target);
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

          
            drag.OnBadDrop = () =>
            {
                var p = CookingPrepListUI.Instance;
                bool setAside = p != null && p.IsSetAsideStepCurrent();
                drag.suppressBadDropMistake = setAside;
                if (setAside && drag.OriginalParent == target) WarnNoPenalty(misdropMessage);
            };

       
            drag.CanBeginDrag = () =>
            {
          
                if (drag.transform.parent != target) return true;

                var prep = CookingPrepListUI.Instance;
                if (prep == null || !prep.strictOrder) return true;

                if (!prep.IsSetAsideStepCurrent()) return true;

                if (prep.CurrentStepMentions(data.id)) return true;

                ScoreManager.Instance?.ReportMistake(prep.wrongStepMessage);
                return false;
            };
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

        bool destroyIngredient = entry != null && entry.destroyIngredientAfterPour;
        bool destroyAnimation = entry != null && entry.destroyAnimationAfterPour;

        GameObject ingredientObj = drag != null ? drag.gameObject : null;
        GameObject animObj = player.gameObject;

        bool started = player.Play(() =>
        {
            if (destroyIngredient)
            {
           
                if (ingredientObj != null) Destroy(ingredientObj);
            }
            else
            {
                if (drag != null) drag.SetLocked(false);
            }

            if (panLiquid != null && entry != null)
                panLiquid.FinishGradualAdd(entry.liquidId, entry.ml);

            Debug.Log($"[PanDrop] Pour finished -> completing '{stepId}'");

            CookingPrepListUI.Instance?.CompleteStep(stepId);

            if (data.id == "Water") liquidSound = PanSound.Boiling;
            else if (data.id == oilId) liquidSound = PanSound.Frying;
            else hasIngredient = true;

            UpdatePanSound();

            OnIngredientAdded?.Invoke(data);
            tutorialTag?.ReportDrop();

            if (destroyAnimation && animObj != null) Destroy(animObj);
        }, data);

        if (started)
        {
            if (destroyIngredient)
            {
            
                drag.MarkDropHandled();  
                drag.SetLocked(true);    
            }
            else
            {
                drag.LockAndReturn();
            }

            if (panLiquid != null && entry != null)
                panLiquid.BeginGradualAdd(entry.liquidId, entry.ml, entry.fillDelay, entry.fillDuration);
        }
    }

    private bool HeatOk(string what)
    {
        if (!requireHeat) return true;

        StoveHeatController s = ResolveStove();
        if (s != null && s.CurrentHeat != StoveHeat.Off) return true;
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