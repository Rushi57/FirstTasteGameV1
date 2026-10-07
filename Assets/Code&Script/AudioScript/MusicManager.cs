using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [SerializeField] AudioSource source;
    [SerializeField] AudioClip mainMusic;
    [SerializeField] AudioClip storyMusic;
    [SerializeField] float fadeTime = 0.6f;
    [SerializeField] AudioMixer mixer;
    AudioClip current;
    Coroutine routine;

    void Awake()
    {
        if(instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        source.loop = true;
    }

    void Start()
    {
        mixer.SetFloat(VolumeSettings.MusicKey, VolumeSettings.ToDb(PlayerPrefs.GetFloat(VolumeSettings.MusicKey, 0.8f)));
        mixer.SetFloat(VolumeSettings.SfxKey, VolumeSettings.ToDb(PlayerPrefs.GetFloat(VolumeSettings.SfxKey, 0.8f)));
        PlayMain();
    }

    public void PlayMain() => Play(mainMusic);
    public void PlayStory() => Play(storyMusic);

    void Play(AudioClip clip)
    {
        if (clip == current) return;
        current = clip;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Swap(clip));
    }
    
    IEnumerator Swap(AudioClip clip)
    {
        float start = source.volume;
        for(float t = 0; t<fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, 0, t / fadeTime);
            yield return null;
        }
        source.clip = clip;
        source.Play();
        for(float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(0, start,t / fadeTime);
            yield return null;
        }
        source.volume = start;
    }
}
