using UnityEngine;

/// One asset per dish. Create via: Right-click in Project > Create > FirstTaste > Dish Reflection
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