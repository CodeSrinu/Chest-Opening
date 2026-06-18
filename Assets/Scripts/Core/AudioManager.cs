using UnityEngine;

public class AudioManager : MonoBehaviour
{
  public static AudioManager Instance { get; private set; }

  [Header("Audio Sources")]
  public AudioSource uiMusicSource;
  public AudioSource gameMusicSource;
  public AudioSource sfxSource;
  public AudioSource movementSource;
  public AudioSource sharkSource;
  public AudioSource shieldSource;
  public AudioClip buttonSource;
  [Header("Music")]
  public AudioClip uiBackgroundMusic;
  public AudioClip gameBackgroundMusic;
  public AudioClip AmbientMusic;

  [Header("UI")]
  public AudioClip buttonClickSound;


  [Header("SFX")]
  public AudioClip treasureCollectClip;
  public AudioClip sharkChaseLoop;
  public AudioClip sharkDamageClip;
  public AudioClip shieldstartLoop;
  public AudioClip shieldstopLoop;
  public AudioClip swimLoopClip;
  public AudioClip gameOverClip;
  public AudioClip obstacleHitClip;


  private const string VolumePrefKey = "SFXVolume";
  private const string MusicPrefKey = "MusicVolume";

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
      return;
    }

    LoadVolumeSettings();
  }

  private void Start()
  {
    PlayAmbientMusic();
  }

  private void LoadVolumeSettings()
  {
    float sfxVolume = PlayerPrefs.GetFloat(VolumePrefKey, 0.8f);
    float musicVolume = PlayerPrefs.GetFloat(MusicPrefKey, 0.5f);

    sfxSource.volume = sfxVolume;
    movementSource.volume = sfxVolume;
    sharkSource.volume = sfxVolume;
    shieldSource.volume = sfxVolume;
    uiMusicSource.volume = musicVolume;
    gameMusicSource.volume = musicVolume;

  }

  public void SetSFXVolume(float value)
  {
    sfxSource.volume = value;
    movementSource.volume = value;
    sharkSource.volume = value;
    shieldSource.volume = value;

    PlayerPrefs.SetFloat(VolumePrefKey, value);
  }

  public void SetMusicVolume(float value)
  {
    uiMusicSource.volume = value;
    gameMusicSource.volume = value;

    PlayerPrefs.SetFloat(MusicPrefKey, value);
  }
  public void PlayAmbientMusic()

  {
    if (gameMusicSource != null && AmbientMusic != null)
    {
      gameMusicSource.clip = AmbientMusic;
      gameMusicSource.loop = true; gameMusicSource.Play();
    }
  }


  public void PlayButtonSound()
  {
    PlaySFX(buttonClickSound);
  }

  public void PlayTreasureCollect()
  {
    PlaySFX(treasureCollectClip);
  }

  public void PlayObstacleHit()
  {
    PlaySFX(obstacleHitClip);
  }


  public void PlaySwimLoop()
  {
    PlaySFX(swimLoopClip);
  }


  public void PlayGameOver()
  {
    PlaySFX(gameOverClip);
  }
  public void StartShieldSound()
  {
    PlaySFX(shieldstartLoop);
  }
  public void StopShieldSound()
  {
    PlaySFX(shieldstopLoop);
  }


  public void PlaySharkDamage()
  {
    PlaySFX(sharkDamageClip);
  }


  private void PlaySFX(AudioClip clip)
  {
    if (sfxSource != null && clip != null)
    {
      sfxSource.PlayOneShot(clip);
    }
  }
}
