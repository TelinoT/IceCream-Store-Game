using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Needed for Coroutines!

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("UI Elements")]
    public Image xpCircleFill; 
    public TMP_Text levelText; 

    [Header("Leveling Math")]
    public int baseXPRequired = 100;
    public float xpMultiplierPerLevel = 1.2f;

    public int currentLevel { get; private set; }
    
    // --- NEW: Tracking for smooth animation ---
    private float displayedXP = 0f;
    private Coroutine fillRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Snap the UI instantly on load so it doesn't animate from 0 every time you boot the game
        int totalXP = PlayerPrefs.GetInt("XP", 0);
        if (EconomyManager.Instance != null) totalXP = EconomyManager.Instance.xp;
        
        displayedXP = totalXP; 
        UpdateUIDirectly((int)displayedXP, true);
    }

    public void CalculateLevel()
    {
        int totalXP = PlayerPrefs.GetInt("XP", 0);
        if (EconomyManager.Instance != null) totalXP = EconomyManager.Instance.xp;

        // Stop any currently running animation and start a new one to the new target
        if (fillRoutine != null) StopCoroutine(fillRoutine);
        fillRoutine = StartCoroutine(AnimateXP(totalXP));
    }

    private IEnumerator AnimateXP(int targetXP)
    {
        float duration = 0.5f; // Same snappy duration as your coins
        float elapsed = 0f;
        float startXP = displayedXP;

        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so it keeps animating even if time is paused
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            // Add a smooth curve so it eases into place
            float easeT = t * t * (3f - 2f * t);

            displayedXP = Mathf.Lerp(startXP, targetXP, easeT);
            
            // Calculate and visually update the UI for this exact frame's XP
            UpdateUIDirectly((int)displayedXP, false);
            
            yield return null;
        }

        // Snap exactly to the final value to prevent floating point errors
        displayedXP = targetXP;
        
        // Final update, passing "true" to lock in the actual currentLevel variable
        UpdateUIDirectly(targetXP, true); 
    }

    // This method does the math for whatever temporary XP amount we pass into it
    private void UpdateUIDirectly(int xpAmount, bool saveRealLevel)
    {
        int tempLevel = 1;
        int xpForNextLevel = baseXPRequired;
        int remainingXP = xpAmount;

        // Keep subtracting required XP until we find what level this XP amount equates to
        while (remainingXP >= xpForNextLevel)
        {
            remainingXP -= xpForNextLevel;
            tempLevel++;
            xpForNextLevel = Mathf.RoundToInt(baseXPRequired * Mathf.Pow(xpMultiplierPerLevel, tempLevel - 1));
        }

        // Apply it visually
        if (levelText != null) 
            levelText.text = tempLevel.ToString();
            
        if (xpCircleFill != null) 
            xpCircleFill.fillAmount = (float)remainingXP / xpForNextLevel;
            
        // Only save the actual level variable when we are finished animating or booting up
        if (saveRealLevel) 
        {
            // Optional: Play a level-up sound if the level just increased!
            if (currentLevel > 0 && tempLevel > currentLevel && AudioManager.Instance != null)
            {
                AudioManager.Instance.Play("LevelUp"); // Replace with your sound name
            }
            
            currentLevel = tempLevel;
        }
    }
}