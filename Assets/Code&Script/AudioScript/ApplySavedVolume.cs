using UnityEngine;
using UnityEngine.Audio;

public class ApplySavedVolume : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;

    void Start()
    {
        mixer.SetFloat(VolumeSettings.MusicKey,
            VolumeSettings.ToDb(PlayerPrefs.GetFloat(VolumeSettings.MusicKey, 0.8f)));
        mixer.SetFloat(VolumeSettings.SfxKey,
            VolumeSettings.ToDb(PlayerPrefs.GetFloat(VolumeSettings.SfxKey, 0.8f)));
    }
}