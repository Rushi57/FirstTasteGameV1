using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StarRatingDisplay : MonoBehaviour
{
    public Image starImage;

    [Header("Sprite")]
    public Sprite noStar;
    public Sprite oneStar;
    public Sprite twoStar;
    public Sprite threeStar;

    [Header("Optional reveal animator")]
    public bool animateReveal = true;
    public float stepDelay = 0.4f;

    private void Awake()
    {
        if(starImage != null)
        {
            starImage.preserveAspect = true;
        }
    }

    private void OnEnable()
    {
        int stars = ScoreManager.Instance != null ? ScoreManager.Instance.GetStarRating() : 0;
        StopAllCoroutines();
        if (animateReveal) StartCoroutine(Reveal(stars));
        else starImage.sprite = SpriteFor(stars);
    }
    
    private IEnumerator Reveal(int FinalStars)
    {
        starImage.sprite = noStar;
        for(int  s = 1;  s <= FinalStars; s++)
        {
            yield return new WaitForSecondsRealtime(stepDelay);
            starImage.sprite = SpriteFor(s);
        }
    }

    private Sprite SpriteFor(int stars)
    {
        switch (stars)
        {

            case 1: return oneStar;
            case 2: return twoStar;
            case 3: return threeStar;
            default: return null;
        };

    }
}
