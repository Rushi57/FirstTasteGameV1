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

    private int requiredStars = 2;

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
        bool unlocked = requiredLevel == null || GetStars(requiredLevel) >= requiredStars;

        if (button != null) button.interactable = unlocked;
        if (iconImage != null)
            iconImage.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    private int GetStars(LevelData level)
    {
        if (level == null)
            return 0;

        SaveData data = GameSession.GetOrCreateData();

        return data.GetLevelStars(level.levelNumber);
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