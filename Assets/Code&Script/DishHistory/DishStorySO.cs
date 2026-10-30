using UnityEngine;

[CreateAssetMenu(fileName = "NewDishStory", menuName = "Game/Dish Story")]
public class DishStorySO : ScriptableObject
{
    const int MaxChars = 130;

    [System.Serializable]

    public class StoryLine
    {
        [TextArea(2, 5)] public string text;
        public Sprite characterSprite;
    }

    public StoryLine[] lines;

    void OnValidate()
    {
        if (lines == null) return;
        for(int i = 0; i < lines.Length; i++)
        {
            if (lines[i].text != null && lines[i].text.Length > MaxChars)
                Debug.LogWarning($"{name}: line {i + 1} is {lines[i].text.Length} chars (max {MaxChars}), it may be truncated.", this);
        }
    }
}

