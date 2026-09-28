using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Put this on every button inside TbpsContent / CupContent (Tbps, Tbps (1), Cup, Cup (1) ...).
/// Pick a Preset in the Inspector; the label text and ml value are filled in automatically.
[RequireComponent(typeof(Button))]
public class MeasureOptionButton : MonoBehaviour
{
    public enum Preset
    {
        Tsp_1_8, Tsp_1_4, Tsp_1_2, Tsp_1, Tbsp_1_2, Tbsp_1,
        Cup_1_4, Cup_1_3, Cup_1_2, Cup_1
    }

    public Preset preset = Preset.Tsp_1;
    public TMP_Text valueText;                    // the "Value" text; auto-found if empty
    public SeasoningPourController controller;    // auto-found in parents if empty

    public float AmountMl { get; private set; }
    public string Label { get; private set; }

    void Awake()
    {
        Apply();
        if (controller == null)
            controller = GetComponentInParent<SeasoningPourController>(true);
        GetComponent<Button>().onClick.AddListener(() => controller.SelectMeasure(AmountMl, Label));
    }

    void OnValidate() { Apply(); }   // updates the text in the editor too

    void Apply()
    {
        switch (preset)
        {
            case Preset.Tsp_1_8: AmountMl = 0.63f; Label = "1/8 tsp"; break;
            case Preset.Tsp_1_4: AmountMl = 1.25f; Label = "1/4 tsp"; break;
            case Preset.Tsp_1_2: AmountMl = 2.5f; Label = "1/2 tsp"; break;
            case Preset.Tsp_1: AmountMl = 5f; Label = "1 tsp"; break;
            case Preset.Tbsp_1_2: AmountMl = 7.5f; Label = "1/2 tbsp"; break;
            case Preset.Tbsp_1: AmountMl = 15f; Label = "1 tbsp"; break;
            case Preset.Cup_1_4: AmountMl = 60f; Label = "1/4 cup"; break;
            case Preset.Cup_1_3: AmountMl = 85f; Label = "1/3 cup"; break;
            case Preset.Cup_1_2: AmountMl = 125f; Label = "1/2 cup"; break;
            case Preset.Cup_1: AmountMl = 250f; Label = "1 cup"; break;
        }

        if (valueText == null) valueText = GetComponentInChildren<TMP_Text>(true);
        if (valueText != null) valueText.text = Label;
    }
}