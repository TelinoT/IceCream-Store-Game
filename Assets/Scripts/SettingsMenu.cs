using UnityEngine;
using UnityEngine.UI; // Required for handling Sliders

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Slider sensitivitySlider;

    // Keys for saving data
    private const string SensitivityKey = "CamSensitivity";
    private const float DefaultSensitivity = 0.2f;

    void Start()
    {
        // 1. Load the saved value (or use default if first time playing)
        float savedValue = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);

        // 2. Update the Slider UI to match
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedValue;
            
            // Add a listener so the code runs every time the slider moves
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        // 3. Apply the value to the Camera immediately
        ApplySensitivity(savedValue);
    }

    // Called automatically when Slider moves
    public void OnSensitivityChanged(float newValue)
    {
        ApplySensitivity(newValue);
        
        // Save immediately
        PlayerPrefs.SetFloat(SensitivityKey, newValue);
        PlayerPrefs.Save();
    }

    void ApplySensitivity(float value)
    {
        if (CameraSwipeMover.Instance != null)
        {
            CameraSwipeMover.Instance.SetSensitivityFactor(value);
        }
    }
}