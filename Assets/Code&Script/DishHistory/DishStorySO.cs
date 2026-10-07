using UnityEngine;

[CreateAssetMenu(fileName = "NewDishStory", menuName = "Game/Dish Story")]
public class DishStorySO : ScriptableObject
{
    [System.Serializable]

    public class StoryLine
    {
        [TextArea(2, 5)] public string text;
        public Sprite characterSprite;
    }

    public StoryLine[] lines;
}
