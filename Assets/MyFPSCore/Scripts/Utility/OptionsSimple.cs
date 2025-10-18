using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsSimple : MonoBehaviour
{
    [Header("Optional UI (assign only what you use)")]
    [SerializeField] Slider volumeSlider;       // 0..1
    [SerializeField] Slider sensitivitySlider;  // e.g., 0.1..3

    const string VOL_KEY = "settings.masterVolume";
    const string SENS_KEY = "settings.mouseSensitivity";

    void OnEnable()
    {
        // Load saved values with safe defaults
        float vol = PlayerPrefs.GetFloat(VOL_KEY, 0.8f);
        float sens = PlayerPrefs.GetFloat(SENS_KEY, 1.0f);

        if (volumeSlider) volumeSlider.value = Mathf.Clamp01(vol);
        if (sensitivitySlider) sensitivitySlider.value = Mathf.Max(0.05f, sens);

        // Apply volume immediately at panel open
        AudioListener.volume = Mathf.Clamp01(vol);
    }

    // Hook this to Volume slider's OnValueChanged (float)
    public void OnVolumeChanged(float v)
    {
        float clamped = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(VOL_KEY, clamped);
        AudioListener.volume = clamped;
        // PlayerPrefs.Save(); // optional
    }

    // Hook this to Sensitivity slider's OnValueChanged (float)
    public void OnSensitivityChanged(float v)
    {
        float clamped = Mathf.Max(0.05f, v);
        PlayerPrefs.SetFloat(SENS_KEY, clamped);
        // PlayerPrefs.Save(); // optional
    }
}
