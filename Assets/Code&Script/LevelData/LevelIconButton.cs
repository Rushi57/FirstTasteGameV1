using UnityEngine;
using UnityEngine.UI;

public class LevelIconButton : MonoBehaviour
{
    public LevelData levelData;
    public DashboardStarDisplay dashboardStars;
    public Image dishImage;

    [Header("Locking")]
    [Tooltip("Level that must be completed (1+ star) to unlock this one. Leave EMPTY for Level 1.")]
    public LevelData requiredLevel;

    private Button button;
    private Image iconImage;

    private void Awake()
    {
        button = GetComponent<Button>();
        iconImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        RefreshLock();
    }

    public void RefreshLock()
    {
        bool unlocked = requiredLevel == null || GetStars(requiredLevel) > 0;

        if (button != null) button.interactable = unlocked;
        if (iconImage != null)
            iconImage.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    private int GetStars(LevelData level)
    {
        // >>> REPLACE this line with however your game reads saved stars <<<
        return PlayerPrefs.GetInt($"Stars_Level{level.levelNumber}", 0);
    }

    public void SelectLevel()
    {
        if (levelData == null)
        {
            Debug.LogWarning($"[LevelIconButton] {gameObject.name} has no LevelData assigned.");
            return;
        }

        LevelSelectionManager.SelectLevel(levelData);
        dashboardStars?.Refresh(levelData);
        ShowDishImage();
    }

    private void ShowDishImage()
    {
        if (dishImage == null) return;

        Sprite preview = levelData.levelPreviewImage;
        dishImage.sprite = preview;
        dishImage.preserveAspect = true;
        dishImage.enabled = preview != null;
    }
}