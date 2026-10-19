using UnityEngine;

public class StoveSmokeController : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem smoke;
    public StoveDropZone stoveDropZone;
    public PanDropZone panDropZone;
    public StoveHeatController heatController;

    private bool hasIngredient;

    private void OnEnable()
    {
        if (stoveDropZone != null)
        {
            stoveDropZone.OnPanPlaced += HandlePanPlaced;
            stoveDropZone.OnStoveReset += HandleReset;
        }
        if (panDropZone != null) panDropZone.OnIngredientAdded += HandleIngredient;
        if (heatController != null) heatController.OnHeatChanged += HandleHeat;
    }

    private void OnDisable()
    {
        if (stoveDropZone != null)
        {
            stoveDropZone.OnPanPlaced -= HandlePanPlaced;
            stoveDropZone.OnStoveReset -= HandleReset;
        }
        if (panDropZone != null) panDropZone.OnIngredientAdded -= HandleIngredient;
        if (heatController != null) heatController.OnHeatChanged -= HandleHeat;
    }

    private void Start() => Refresh();

    private void HandlePanPlaced(GameObject pan) => Refresh();
    private void HandleHeat(StoveHeat heat) => Refresh();

    private void HandleIngredient(IngredientData data)
    {
        hasIngredient = true;
        Refresh();
    }

    private void HandleReset()
    {
        hasIngredient = false;
        Refresh();
    }

    private void Refresh()
    {
        if (smoke == null) return;

        bool shouldSmoke =
            stoveDropZone != null && stoveDropZone.HasPan &&
            hasIngredient &&
            heatController != null && heatController.CurrentHeat != StoveHeat.Off;

        if (shouldSmoke && !smoke.isPlaying)
            smoke.Play();
        else if (!shouldSmoke && smoke.isPlaying)
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting); // existing smoke fades out
    }
}