using UnityEngine;

public class StoveSmokeController : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem smoke;
    public StoveDropZone stoveDropZone;
    public PanDropZone panDropZone;
    public StoveHeatController heatController;

    private int solidCount;   // solid ingredients currently in the pan (oil/water don't count)

    private void OnEnable()
    {
        if (stoveDropZone != null)
        {
            stoveDropZone.OnPanPlaced += HandlePanPlaced;
            stoveDropZone.OnStoveReset += HandleReset;
        }
        if (panDropZone != null)
        {
            panDropZone.OnIngredientAdded += HandleAdded;
            panDropZone.OnIngredientRemoved += HandleRemoved;
        }
        if (heatController != null) heatController.OnHeatChanged += HandleHeat;
    }

    private void OnDisable()
    {
        if (stoveDropZone != null)
        {
            stoveDropZone.OnPanPlaced -= HandlePanPlaced;
            stoveDropZone.OnStoveReset -= HandleReset;
        }
        if (panDropZone != null)
        {
            panDropZone.OnIngredientAdded -= HandleAdded;
            panDropZone.OnIngredientRemoved -= HandleRemoved;
        }
        if (heatController != null) heatController.OnHeatChanged -= HandleHeat;
    }

    private void Start() => Refresh();

    private void HandlePanPlaced(GameObject pan) => Refresh();
    private void HandleHeat(StoveHeat heat) => Refresh();

    private void HandleAdded(IngredientData data)
    {
        if (data != null && data.playsPourAnimation) return;   // oil / water
        solidCount++;
        Refresh();
    }

    private void HandleRemoved(IngredientData data)
    {
        if (data != null && data.playsPourAnimation) return;
        solidCount = Mathf.Max(0, solidCount - 1);
        Refresh();
    }

    private void HandleReset()
    {
        solidCount = 0;
        Refresh();
    }

    private void Refresh()
    {

        if (smoke == null) return;

        bool shouldSmoke =
            stoveDropZone != null && stoveDropZone.HasPan &&
            solidCount > 0 &&
            heatController != null && heatController.CurrentHeat != StoveHeat.Off;

        if (shouldSmoke)
        {
            // isEmitting (not isPlaying): after Stop(StopEmitting) the system still
            // "plays" while old particles fade, which blocked the restart.
            if (!smoke.isEmitting) smoke.Play();
        }
        else
        {
            if (smoke.isEmitting)
                smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}