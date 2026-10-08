using UnityEngine;
using System.Collections.Generic;
using System;
[CreateAssetMenu(fileName = "DishLiquidProfile", menuName = "Cooking/Dish Liquid Profile")]
public class DishLiquidProfile : ScriptableObject
{
    [Serializable]
    public class Liquid
    {
        [Tooltip("Exact id: Soy, Vinegar, CookingOil, Water, PorkBlood, SquidInk, Broth, Egg, Annatto...")]
        public string id;
        public Color color = Color.white;
        [Tooltip("How strongly it takes over the color. Water ~0.1, soy ~8, ink ~15.")]
        public float strength = 1f;
        [Range(0f, 1f)] public float opacity = 0.6f;
    }

    [Header("Look of this dish")]
    public bool showLiquid = true;
    public Color baseColor = new Color(0.8f, 0.8f, 0.9f);
    [Range(0f, 1f)] public float baseOpacity = 0.3f;
    public Color reducedColor = new Color(0.2f, 0.1f, 0.04f);
    [Tooltip("Higher = the strongest liquid takes over fast")]
    public float sensitivity = 1.2f;

    public List<Liquid> liquids = new List<Liquid>();

    public Liquid Get(string  id)
    {
         foreach (var l in liquids) if (l.id == id) return l;
        return null;
    }
}
