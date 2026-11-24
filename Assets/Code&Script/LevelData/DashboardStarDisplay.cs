using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashboardStarDisplay : MonoBehaviour
{
    public Image starImage;
    public Sprite noStar, oneStar, twoStar, threeStars;

    [Header("Title")]
    public TMP_Text titleText;
    public string lockedTitle = "???";

    private void Start()
    {
        Refresh(LevelSelectionManager.SelectedLevel);
    }
    public void Refresh(LevelData level)
    {
        if (level == null) { starImage.sprite = noStar; return; }

        SaveData data = GameSession.GetOrCreateData();
        int stars = data.GetLevelStars(level.levelNumber);
        starImage.preserveAspect = true;
        starImage.sprite = stars switch
        {
            1 => oneStar,
            2 => twoStar,
            3 => threeStars,
            _ => noStar
        };
        if (titleText != null)
            titleText.text = stars > 0 ? level.levelName : lockedTitle;
    }
}
