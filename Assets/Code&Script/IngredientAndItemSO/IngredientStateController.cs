using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class IngredientStateController : MonoBehaviour
{
    public IngredientData data;
    public Image image;

    public IngredientPrepState currentState { get; private set; } = IngredientPrepState.Whole;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
    }

    public void Initialize(IngredientData ingredientData)
    {
        data = ingredientData;
        SetState(IngredientPrepState.Whole);
    }

    public void SetState(IngredientPrepState newState)
    {
        currentState = newState;

        if (data == null || image == null) return;

        Sprite sprite = data.GetSpriteForState(currentState);
        if(sprite != null)
            image.sprite = sprite;
    }
}
