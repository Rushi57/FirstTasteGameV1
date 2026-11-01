using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put on each LevelIcon in the map scene. Shows a check marker
/// when the level has been finished with enough stars.
/// </summary>
public class LevelIconCompletionMarker : MonoBehaviour
{
    [Header("Which level this icon represents")]
    public LevelData level;

    [Header("Option A: swap the icon's sprite")]
    public Image iconImage;          // the Image showing the "?" icon
    public Sprite completedSprite;   // drag "check marker" here

    [Header("Option B: show an overlay instead (optional)")]
    public GameObject checkOverlay;  // a child object with the check image

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