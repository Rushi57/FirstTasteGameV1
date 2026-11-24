using UnityEngine;
using UnityEngine.UI;

public class LevelIconCompletionMarker : MonoBehaviour
{
    [Header("Which level this icon represents")]
    public LevelData level;

    [Header("Option A: swap the icon's sprite")]
    public Image iconImage;        
    public Sprite completedSprite;   

    [Header("Option B: show an overlay instead (optional)")]
    public GameObject checkOverlay;  

    [Header("Rules")]
    public int minStarsToComplete = 2;

    private Sprite originalSprite;

    private void Awake()
    {
        if (iconImage == null) iconImage = GetComponent<Image>();
        if (iconImage != null) originalSprite = iconImage.sprite;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        bool completed = false;

        if (level != null)
        {
            SaveData data = GameSession.GetOrCreateData();
            completed = data.GetLevelStars(level.levelNumber) >= minStarsToComplete;
        }

        if (checkOverlay != null)
            checkOverlay.SetActive(completed);

        if (iconImage != null && completedSprite != null)
        {
            iconImage.sprite = completed ? completedSprite : originalSprite;
            iconImage.preserveAspect = true;
        }
    }
}