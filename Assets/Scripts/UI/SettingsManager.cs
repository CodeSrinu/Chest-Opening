using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private void Awake()
    {
        sfxSlider?.onValueChanged.AddListener(SetSFXVolume);
        musicSlider?.onValueChanged.AddListener(SetMusicVolume);
    }

    public void SetSFXVolume(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }
}
