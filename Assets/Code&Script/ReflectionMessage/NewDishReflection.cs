using UnityEngine;


[CreateAssetMenu(fileName = "NewDishReflection", menuName = "FirstTaste/Dish Reflection")]
public class DishReflectionData : ScriptableObject
{
    [Header("Dish")]
    public string dishName = "Adobo";
    public Sprite dishSprite;

    [Header("Reflection pages (one short paragraph each)")]
    [TextArea(3, 8)]
    public string[] pages;
}