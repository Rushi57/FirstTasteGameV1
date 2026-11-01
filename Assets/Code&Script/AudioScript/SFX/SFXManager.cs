using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Mixer")]
    public AudioMixerGroup sfxGroup;
    public AudioMixer mixer;

    [Header("One-Shot Clip")]
    public AudioClip potLidClose;
    public AudioClip pouringWater;
    public AudioClip settingCastIron;
    public AudioClip stirringPot;
    public AudioClip tapButtonTest;
    public AudioClip errorAndWrong;
    public AudioClip gameOverPanel;
    public AudioClip scorebordePanel;
    public AudioClip tickingStoveSound;
    public AudioClip pourAnyLiquid;
    public AudioClip ChopIngredientSound;
    public AudioClip jarScopping;
    public AudioClip fridgeOpen;
    public AudioClip fridgeClose;
    public AudioClip cabinetClose;
    public AudioClip cabinetOpen;

    

    [Header("Looping clips")]
    public AudioClip frying;
    public AudioClip boilingLiquid;
    public AudioClip boilingWater;
    public AudioClip clockTicking;
    public AudioClip stoveFlameSound;

    AudioSource oneShotSource;
    AudioSource loopSource;
    UnityAction tapAction;
    AudioSource stirSource;
    AudioSource tickSource;
    AudioSource pourSource;
    AudioSource clockSource;

    private AudioSource flameSource;

    [Header("Stove Flame Levels")]
    public float lowVolume = 0.25f;
    public float mediumVolume = 0.5f;
    public float highVolume = 0.85f;
    public float lowPitch = 0.9f;
    public float mediumPitch = 1.0f;
    public float highPitch = 1.15f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.outputAudioMixerGroup = sfxGroup;
        oneShotSource.playOnAwake = false;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.outputAudioMixerGroup = sfxGroup;
        loopSource.loop = true;          // needed for frying/boiling to repeat
        loopSource.playOnAwake = false;

        stirSource = gameObject.AddComponent<AudioSource>();
        stirSource.outputAudioMixerGroup = sfxGroup;
        stirSource.loop = true;
        stirSource.playOnAwake = false;

        tickSource = gameObject.AddComponent<AudioSource>();
        tickSource.outputAudioMixerGroup = sfxGroup;
        tickSource.loop = true;
        tickSource.playOnAwake = false;

        pourSource = gameObject.AddComponent<AudioSource>();
        pourSource.outputAudioMixerGroup = sfxGroup;
        pourSource.loop = true;
        pourSource.playOnAwake=false;

        clockSource = gameObject.AddComponent<AudioSource>();
        clockSource.outputAudioMixerGroup = sfxGroup;
        clockSource.loop = true;
        clockSource.playOnAwake = false;

        tapAction = PlayTap;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() => HookAllButtons();

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HookAllButtons();

    // Adds the tap sound to every button, including ones in inactive panels
    public void HookAllButtons()
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            b.onClick.RemoveListener(tapAction);
            if (b.GetComponent<NoTapSound>() != null) continue; // opt-out marker
            b.onClick.AddListener(tapAction);
        }
    }
    public void PlayChop() => Play(ChopIngredientSound);
    public void PlayFridgeOpen() => Play(fridgeOpen);
    public void PlayFridgeClose() => Play(fridgeClose);
    public void PlayCabinetOpen() => Play(cabinetOpen);
    public void PlayCabinetClose() => Play(cabinetClose);

    public void PlayJarScoop() => Play(jarScopping);
    public void PlayTap() => Play(tapButtonTest);
    
    public void PlayPotLidClose() => Play(potLidClose);
    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null) oneShotSource.PlayOneShot(clip, volume);
    }

    public void PlayLoop(AudioClip clip)
    {
        if (clip == null || (loopSource.isPlaying && loopSource.clip == clip)) return;
        Debug.Log($"[SFX] PlayLoop({clip.name})\n{System.Environment.StackTrace}");
        loopSource.clip = clip;
        loopSource.Play();
    }

    public void StartStir()
    {
        if (stirringPot == null || stirSource.isPlaying) return;
        stirSource.clip = stirringPot;
        stirSource.Play();
    }

    public void StopStir()
    {
        if(stirSource.isPlaying) stirSource.Stop();
    }


    public void StartTick()
    {
        if (tickingStoveSound == null || tickSource.isPlaying) return;
        tickSource.clip = tickingStoveSound;
        tickSource.Play();
    }
    public void StopTick()
    {
        if(tickSource.isPlaying) tickSource.Stop();
    }

    public void StopLoop() => loopSource.Stop();
    public bool IsLoopPlaying => loopSource.isPlaying;
    public void StartPour()
    {
        if(pourAnyLiquid == null || pourSource.isPlaying) return;  
        pourSource.clip = pourAnyLiquid;
        pourSource.Play();
    }
    public void StopPour()
    {
        if(pourSource.isPlaying) pourSource.Stop();
    }
    public void StartClock()
    {
        if (clockTicking == null || clockSource.isPlaying) return;
        clockSource.clip = clockTicking;
        clockSource.Play();
    }

    public void StopClock()
    {
        if (clockSource.isPlaying) clockSource.Stop();
    }

    //Stove
    public void SetFlame(StoveHeat heat)
    {
        if(heat == StoveHeat.Off)
        {
            StopFlame();
            return;
        }    

        if(flameSource == null)
        {
            flameSource = gameObject.AddComponent<AudioSource>();
            flameSource.outputAudioMixerGroup = sfxGroup;   // your SFX mixer group field
            flameSource.clip = stoveFlameSound;             // your flame clip field
            flameSource.loop = true;
            flameSource.playOnAwake = false;
        }

        switch(heat)
        {
            case StoveHeat.Low:
                flameSource.volume = lowVolume;
                flameSource.pitch = lowPitch;
                break;
            case StoveHeat.Medium:
                flameSource.volume = mediumVolume;
                flameSource.pitch = mediumPitch;
                break;
            case StoveHeat.High:
                flameSource.volume = highVolume;
                flameSource.pitch = highPitch;
                break;

        }
        if (!flameSource.isPlaying) flameSource.Play();
    }

    public void StopFlame()
    {
        if (flameSource != null && flameSource.isPlaying) flameSource.Stop();
    }





    public class NoTapSound : MonoBehaviour { }
}