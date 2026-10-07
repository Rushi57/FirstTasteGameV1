using UnityEngine;
using UnityEngine.UI;

public class PreserveAllImages : MonoBehaviour
{
    void Awake()
    {
        Image[] images = FindObjectsOfType<Image>();

        foreach (Image image in images)
        {
            image.preserveAspect = true;
        }
    }
}