using UnityEngine;
using TMPro;

public enum SpoonMeasurement { Tbsp_1, Tbsp_1_2, Tsp_1, Tsp_1_2, Tsp_1_4, Tsp_1_8,
    Cup_1, Cup_1_2, Cup_1_3, Cup_1_4
}

public static class SpoonMeasurementTable
{
    public const float TspPerCup = 48f;
    public static string Label(SpoonMeasurement m) => m switch
    {
       
        SpoonMeasurement.Tbsp_1 => "1 tbsp",
        SpoonMeasurement.Tbsp_1_2 => "1/2 tbsp",
        SpoonMeasurement.Tsp_1 => "1 tsp",
        SpoonMeasurement.Tsp_1_2 => "1/2 tsp",
        SpoonMeasurement.Tsp_1_4 => "1/4 tsp",
        SpoonMeasurement.Tsp_1_8 => "1/8 tsp",
        SpoonMeasurement.Cup_1 => "1 cup",
        SpoonMeasurement.Cup_1_2 => "1/2 cup",
        SpoonMeasurement.Cup_1_3 => "1/3 cup",
        SpoonMeasurement.Cup_1_4 => "1/4 cup",
        _ => "1/8 tsp"
    };

    public static float TspValue(SpoonMeasurement m) => m switch
    {
        SpoonMeasurement.Tbsp_1 => 3f,
        SpoonMeasurement.Tbsp_1_2 => 1.5f,
        SpoonMeasurement.Tsp_1 => 1f,
        SpoonMeasurement.Tsp_1_2 => 0.5f,
        SpoonMeasurement.Tsp_1_4 => 0.25f,
        SpoonMeasurement.Tsp_1_8 => 0.125f,
        SpoonMeasurement.Cup_1 => TspPerCup,
        SpoonMeasurement.Cup_1_2 => TspPerCup / 2f,
        SpoonMeasurement.Cup_1_3 => TspPerCup / 3f,
        SpoonMeasurement.Cup_1_4 => TspPerCup / 4f,
        _ => 0.125f
    };
}

public class SaltPepperSpoonItem : MonoBehaviour
{
    public SpoonMeasurement measurement = SpoonMeasurement.Tsp_1;
    public TMP_Text valueLabel;

    public float TspValue => SpoonMeasurementTable.TspValue(measurement);
    public string Label => SpoonMeasurementTable.Label(measurement);

    /// <summary>The spoon's home slot (its Container), cached before any dragging can move it.</summary>
    public RectTransform HomeParent { get; private set; }

    private void Awake()
    {
        HomeParent = transform.parent as RectTransform;

        if (valueLabel == null && transform.parent != null)
            valueLabel = transform.parent.GetComponentInChildren<TMP_Text>(true);

        if (valueLabel != null)
            valueLabel.text = Label;
    }
}