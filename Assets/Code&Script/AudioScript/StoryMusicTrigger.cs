using UnityEngine;

public class StoryMusicTrigger : MonoBehaviour
{
    void OnEnable()
    {
        if (MusicManager.instance != null) MusicManager.instance.PlayStory();
        
    }
    void OnDisable()
    {
        if (MusicManager.instance != null) MusicManager.instance.PlayMain();    
    }
}
