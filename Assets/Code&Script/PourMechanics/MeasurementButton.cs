using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MeasurementOption
{
    // Spoon scroll
    Tsp_1_8, Tsp_1_4, Tsp_1_2, Tsp_1, Tbsp_1_2, Tbsp_1,
    // Cup scroll
    Cup_1_4, Cup_1_3, Cup_1_2, Cup_1
}

/// <summary>Single source of truth for your measurement chart.</summary>
public static class MeasurementTable
{
    public static string Label(MeasurementOption o) => o switch
    {
        MeasurementOption.Tsp_1_8 => "1/8 tsp",
        MeasurementOption.Tsp_1_4 => "1/4 tsp",
        MeasurementOption.Tsp_1_2 => "1/2 tsp",
        MeasurementOption.Tsp_1 => "1 tsp",
        MeasurementOption.Tbsp_1_2 => "1/2 tbsp",
        MeasurementOption.Tbsp_1 => "1 tbsp",
        MeasurementOption.Cup_1_4 => "1/4 cup",
        MeasurementOption.Cup_1_3 => "1/3 cup",
        MeasurementOption.Cup_1_2 => "1/2 cup",
        _ => "1 cup"
    };

    public static float Ml(MeasurementOption o) => o switch
    {
        MeasurementOption.Tsp_1_8 => 0.63f,
        MeasurementOption.Tsp_1_4 => 1.25f,
        MeasurementOption.Tsp_1_2 => 2.5f,
        MeasurementOption.Tsp_1 => 5f,
        MeasurementOption.Tbsp_1_2 => 7.5f,
        MeasurementOption.Tbsp_1 => 15f,
        MeasurementOption.Cup_1_4 => 60f,
        MeasurementOption.Cup_1_3 => 85f,
        MeasurementOption.Cup_1_2 => 125f,
        _ => 250f
    };
}

[RequireComponent(typeof(Button))]
public class MeasurementButton : MonoBehaviour
{
    [Tooltip("Pick which measurement this button represents.")]
    public MeasurementOption option = MeasurementOption.Tbsp_1;

    [Tooltip("The TMP text that shows the value (the 'Value' placeholder).")]
    public TMP_Text label;
    public bool showMl = false;

    [Header("Selected look")]
    public Image background;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);

    public float Ml => MeasurementTable.Ml(option);
    public string Label => MeasurementTable.Label(option);

    public event Action<MeasurementButton> Clicked;

    private bool initialized;

    private void Init()
    {
        if (initialized) return;
        initialized = true;

        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = showMl ? $"{Label} ({Ml:0.##} ml)" : Label;

        GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));
    }

    private void Awake() => Init();

    public void SetSelected(bool selected)
    {
        Init();
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}