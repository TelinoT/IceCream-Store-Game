using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PatienceUIManager : MonoBehaviour
{
    public static PatienceUIManager Instance;

    [Header("Main Canvas References")]
    public Slider slider;
    public Image fill;
    public GameObject serveUI;
    public GameObject startPrepareUI;
    
   [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public float offScreenYOffset = 500f; 

    // --- NEW: Shake Settings ---
    [Header("Shake Settings")]
    public float maxShakeAmount = 8f; // The maximum pixels it will jump around
    [Tooltip("At what percentage should it start trembling? (0.5 = 50%)")]
    public float shakeStartPercentage = 0.5f;

    private RectTransform sliderRect;
    private Vector2 originalPos;
    private Vector2 offScreenPos;
    private Coroutine currentAnim;
    
    // --- NEW: Safety switch to prevent shaking while sliding ---
    private bool isAnimating = false;

    void Awake()
    {
        Instance = this;
        
        if (slider != null)
        {
            sliderRect = slider.GetComponent<RectTransform>();
            originalPos = sliderRect.anchoredPosition;
            offScreenPos = new Vector2(originalPos.x, originalPos.y + offScreenYOffset);
            
            sliderRect.anchoredPosition = offScreenPos;
            slider.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // --- NEW: The Shake Logic! ---
        // Only run this if the slider is actually sitting on the screen waiting
        if (!isAnimating && slider != null && slider.gameObject.activeSelf && slider.maxValue > 0)
        {
            float percentage = slider.value / slider.maxValue;

            // Have we dropped below the warning threshold?
            if (percentage <= shakeStartPercentage)
            {
                // Calculate intensity (0.0 when it first hits the threshold, 1.0 when time is at zero)
                float shakeIntensity = 1f - (percentage / shakeStartPercentage);
                float currentShake = maxShakeAmount * shakeIntensity;

                // Generate a random pixel offset
                Vector2 shakeOffset = new Vector2(
                    Random.Range(-currentShake, currentShake),
                    Random.Range(-currentShake, currentShake)
                );

                // Apply the jitter on top of its normal resting position
                sliderRect.anchoredPosition = originalPos + shakeOffset;
            }
            else
            {
                // Ensure it stays perfectly still if we have plenty of time
                sliderRect.anchoredPosition = originalPos;
            }
        }
    }

    public void ShowSlider(float maxValue)
    {
        if (slider != null)
        {
            slider.gameObject.SetActive(true);
            slider.maxValue = maxValue;
            slider.value = maxValue;

            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(SlideTo(originalPos, true));
        }
    }

    public void UpdateSlider(float currentValue, Color currentColor)
    {
        if (slider != null) slider.value = currentValue;
        if (fill != null) fill.color = currentColor;
    }

    public void HideSlider()
    {
        if (slider != null && slider.gameObject.activeSelf)
        {
            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(SlideTo(offScreenPos, false));
        }
    }

    private IEnumerator SlideTo(Vector2 targetPos, bool isSlidingIn)
    {
        isAnimating = true; // Lock the shake!
        
        float elapsed = 0f;
        Vector2 startPos = sliderRect.anchoredPosition;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            
            float easeT = isSlidingIn ? 1f - Mathf.Pow(1f - t, 3f) : t * t * t;

            sliderRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
            yield return null;
        }

        sliderRect.anchoredPosition = targetPos;
        isAnimating = false; // Unlock the shake!

        if (!isSlidingIn)
        {
            slider.gameObject.SetActive(false);
        }
    }
    public void SwitchServeUI()
    {
        startPrepareUI.SetActive(true);
        serveUI.SetActive(false);
    }
}