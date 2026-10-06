using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public const string MusicKey = "MusicVol";
    public const string SfxKey = "SfxVol";

    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    void OnEnable()
    {
        // Show the saved value without triggering the listener
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicKey, 0.8f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxKey, 0.8f));

        musicSlider.onValueChanged.AddListener(SetMusic);
        sfxSlider.onValueChanged.AddListener(SetSfx);
    }

    void OnDisable()
    {
        musicSlider.onValueChanged.RemoveListener(SetMusic);
        sfxSlider.onValueChanged.RemoveListener(SetSfx);
        PlayerPrefs.Save();
    }

    public void SetMusic(float v)
    {
        mixer.SetFloat(MusicKey, ToDb(v));
        PlayerPrefs.SetFloat(MusicKey, v);
    }

    public void SetSfx(float v)
    {
        mixer.SetFloat(SfxKey, ToDb(v));
        PlayerPrefs.SetFloat(SfxKey, v);
    }

    public static float ToDb(float v) => Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;
}