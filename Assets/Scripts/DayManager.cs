using UnityEngine;
using TMPro;
using System.Collections; 

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;
    
    public UpgradeUIManager upgradeManagerUI;

    [Header("Day Settings")]
    public int currentDay = 1;
    public int baseCustomersPerDay = 5;
    public int customerVariance = 2;
    
    private int targetCustomersToday;
    private int customersServedToday;
    
    public bool isDayActive { get; private set; } = false;
    public bool isBetweenDays { get; private set; } = false; 

    [Header("Economy Tracking")]
    private int coinsAtStartOfDay;
    private int xpAtStartOfDay;

    [Header("Clock Settings")]
    public float openingHour = 9f;  
    public float closingHour = 18f; 
    public float baseClockSpeed = 0.05f; 
    public float catchUpMultiplier = 2f; 
    private float currentTimeOfDay;

    [Header("In-World UI References")]
    public TextMeshProUGUI dayText;      
    public TextMeshProUGUI progressText; 
    public TextMeshProUGUI clockText;    

    [Header("Lighting & Environment")]
    public GameObject dayLight;    
    public GameObject nightLight;  
    public Material daySkybox;     
    public Material nightSkybox;   
    
    private float originalDayIntensity;
    private float originalNightIntensity;

    [Header("Phase 1: Summary Panel")]
    public GameObject summaryPanel;
    public RectTransform summaryPanelRect; 
    public TextMeshProUGUI summaryDayText; 
    public TextMeshProUGUI dailyEarningsText;
    public TextMeshProUGUI dailyXPText;
    public TextMeshProUGUI totalCoinsText;
    public GameObject DayTimeUI;

    [Header("Phase 2: Night Hub Panel")]
    public GameObject nightHubPanel;
    public RectTransform nightHubPanelRect;
    public DecorationUIManager shopManager;

    [Header("Camera Transition")]
    public Camera gameplayCamera;
    public Camera nightCameraTarget; 
    public float cameraTransitionDuration = 0.4f;

    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private float originalCamFOV;

    private Vector2 summaryOriginalPos;
    private Vector2 nightHubOriginalPos;
    private Vector2 offScreenTop = new Vector2(0f, 1500f);
    public Vector2 offScreenRight = new Vector2(1500f, 0f); 

    void Awake()
    {
        if (Instance == null) Instance = this;
        LoadDay();
    }

    void Start()
    {
        if (summaryPanelRect != null) summaryOriginalPos = summaryPanelRect.anchoredPosition;
        if (nightHubPanelRect != null) nightHubOriginalPos = nightHubPanelRect.anchoredPosition;
        
        if (dayLight != null) originalDayIntensity = dayLight.GetComponent<Light>().intensity;
        if (nightLight != null) originalNightIntensity = nightLight.GetComponent<Light>().intensity;

        // Pass "true" because the game just started and we want the lights instantly set up!
        StartDay(true); 
    }

    void Update()
    {
        if (isDayActive) UpdateClock();
    }

    private void UpdateClock()
    {
        float progressRatio = (float)customersServedToday / targetCustomersToday;
        float idealTime = Mathf.Lerp(openingHour, closingHour, progressRatio);

        float currentSpeed = baseClockSpeed;
        if (currentTimeOfDay < idealTime)
        {
            float difference = idealTime - currentTimeOfDay;
            currentSpeed += (difference * catchUpMultiplier);
        }

        currentTimeOfDay += currentSpeed * Time.deltaTime;
        currentTimeOfDay = Mathf.Clamp(currentTimeOfDay, openingHour, closingHour);

        if (clockText != null)
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay);
            int rawMinutes = Mathf.FloorToInt((currentTimeOfDay - hours) * 60f);
            int snappedMinutes = (rawMinutes / 15) * 15;
            clockText.text = string.Format("{0:00}:{1:00}", hours, snappedMinutes);
        }
    }

    // --- UPDATED: Added a bool so we can choose if the lights snap or fade ---
    public void StartDay(bool instantSetup = false)
    {
        targetCustomersToday = baseCustomersPerDay + Random.Range(-customerVariance, customerVariance + 1);
        if (targetCustomersToday < 1) targetCustomersToday = 1; 
        
        customersServedToday = 0;
        currentTimeOfDay = openingHour; 
        
        isDayActive = true;
        isBetweenDays = false;

        coinsAtStartOfDay = EconomyManager.Instance.coins;
        xpAtStartOfDay = EconomyManager.Instance.xp;

        UpdateProgressUI();
        if (dayText != null) dayText.text = "Day " + currentDay;

        if (summaryPanel != null) summaryPanel.SetActive(false);
        if (nightHubPanel != null) nightHubPanel.SetActive(false);
        
        // Only instantly snap the lights if we specifically ask to (like when the game boots up)
        if (instantSetup)
        {
            if (dayLight != null) 
            {
                dayLight.SetActive(true);
                dayLight.GetComponent<Light>().intensity = originalDayIntensity; 
            }
            if (nightLight != null) nightLight.SetActive(false);
            if (daySkybox != null) RenderSettings.skybox = daySkybox; 
        }

        Time.timeScale = 1f;
        FindObjectOfType<CustomerManager>().SpawnFirstCustomer();
    }

    public void CustomerServed()
    {
        if (!isDayActive) return;

        customersServedToday++;
        UpdateProgressUI();

        if (customersServedToday >= targetCustomersToday)
        {
            currentTimeOfDay = closingHour;
            UpdateClock(); 
            EndShift();
        }
        else
        {
            FindObjectOfType<CustomerManager>().SpawnNextCustomer();
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null) progressText.text = $"Orders: {customersServedToday} / {targetCustomersToday}";
    }

    public void EndShift()
    {
        isDayActive = false;
        isBetweenDays = true; 
        Time.timeScale = 0f; 
        
        int dailyCoins = EconomyManager.Instance.coins - coinsAtStartOfDay;
        int dailyXP = EconomyManager.Instance.xp - xpAtStartOfDay;
        int totalBank = EconomyManager.Instance.coins;

        if (summaryDayText != null) summaryDayText.text = "Day " + currentDay;

        currentDay++;
        SaveDay();
        
        AudioManager.Instance.Play("Success"); 

        if (summaryPanel != null) 
        {
            summaryPanel.SetActive(true);
            if (summaryPanelRect != null) StartCoroutine(SlidePanelIn(summaryPanelRect, offScreenTop, summaryOriginalPos));
            
            StartCoroutine(TickUpStats(dailyCoins, dailyXP, totalBank));
        }
        
        DayTimeUI.SetActive(false);
        
        TaskManager.Instance.GenerateTomorrowTasks();
    }

    private IEnumerator TickUpStats(int targetCoins, int targetXP, int finalBank)
    {
        float duration = 1.0f; 
        float elapsed = 0f;
        
        int startingBank = finalBank - targetCoins;

        if (dailyEarningsText != null) dailyEarningsText.text = "+ 0 $$";
        if (dailyXPText != null) dailyXPText.text = "+ 0 XP";
        if (totalCoinsText != null) totalCoinsText.text = "Bank: " + startingBank + " $$";

        yield return new WaitForSecondsRealtime(0.2f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            int currentCoins = Mathf.RoundToInt(Mathf.Lerp(0, targetCoins, easeT));
            int currentXP = Mathf.RoundToInt(Mathf.Lerp(0, targetXP, easeT));
            int currentBank = Mathf.RoundToInt(Mathf.Lerp(startingBank, finalBank, easeT));

            if (dailyEarningsText != null) dailyEarningsText.text = "+ " + currentCoins + " $$";
            if (dailyXPText != null) dailyXPText.text = "+ " + currentXP + " XP";
            if (totalCoinsText != null) totalCoinsText.text = "Bank: " + currentBank + " $$";

            yield return null;
        }

        if (dailyEarningsText != null) dailyEarningsText.text = "+ " + targetCoins + " $$";
        if (dailyXPText != null) dailyXPText.text = "+ " + targetXP + " XP";
        if (totalCoinsText != null) totalCoinsText.text = "Bank: " + finalBank + " $$";
    }

    public void TransitionToNight()
    {
        AudioManager.Instance.Play("ButtonPop");
        
        if (summaryPanel != null) summaryPanel.SetActive(false);

        StartCoroutine(FadeLightingToNight());
        StartCoroutine(GlideCameraToNight());
        ShowNightHubPanel();
    }

    private IEnumerator FadeLightingToNight()
    {
        if (dayLight == null || nightLight == null) yield break;

        Light dLight = dayLight.GetComponent<Light>();
        Light nLight = nightLight.GetComponent<Light>();

        dayLight.SetActive(true);
        nightLight.SetActive(true);
        nLight.intensity = 0f; 

        if (nightSkybox != null) RenderSettings.skybox = nightSkybox;

        float elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / cameraTransitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            dLight.intensity = Mathf.Lerp(originalDayIntensity, 0f, easeT);
            nLight.intensity = Mathf.Lerp(0f, originalNightIntensity, easeT);

            yield return null;
        }

        dLight.intensity = 0f;
        nLight.intensity = originalNightIntensity;
        dayLight.SetActive(false);
    }

    public void ShowNightHubPanel()
    {
        if (nightHubPanel != null) 
        {
            nightHubPanel.SetActive(true);
            if (nightHubPanelRect != null) StartCoroutine(SlidePanelIn(nightHubPanelRect, offScreenRight, nightHubOriginalPos));
        }
    }

    public void OpenShopFromNightHub()
    {
        AudioManager.Instance.Play("ButtonPop");
        if (nightHubPanel != null) nightHubPanel.SetActive(false);
        if (shopManager != null) shopManager.EnterShopMode();
    }
    
    public void OpenUpgradesFromNightHub()
    {
        AudioManager.Instance.Play("ButtonPop");
        if (nightHubPanel != null) nightHubPanel.SetActive(false);
        if (upgradeManagerUI != null) upgradeManagerUI.OpenUpgradesMenu();
    }

    private IEnumerator GlideCameraToNight()
    {
        if (gameplayCamera == null || nightCameraTarget == null) yield break;

        originalCamPos = gameplayCamera.transform.position;
        originalCamRot = gameplayCamera.transform.rotation;
        originalCamFOV = gameplayCamera.fieldOfView;

        if (CameraSwipeMover.Instance != null) 
            CameraSwipeMover.Instance.enabled = false;

        float elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / cameraTransitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            gameplayCamera.transform.position = Vector3.Lerp(originalCamPos, nightCameraTarget.transform.position, easeT);
            gameplayCamera.transform.rotation = Quaternion.Lerp(originalCamRot, nightCameraTarget.transform.rotation, easeT);
            gameplayCamera.fieldOfView = Mathf.Lerp(originalCamFOV, nightCameraTarget.fieldOfView, easeT);

            yield return null;
        }

        gameplayCamera.transform.position = nightCameraTarget.transform.position;
        gameplayCamera.transform.rotation = nightCameraTarget.transform.rotation;
        gameplayCamera.fieldOfView = nightCameraTarget.fieldOfView;
    }

    private IEnumerator SlidePanelIn(RectTransform targetRect, Vector2 startPos, Vector2 finalPos)
    {
        targetRect.localScale = Vector3.one;
        targetRect.anchoredPosition = startPos; 
        
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 
            
            targetRect.anchoredPosition = Vector2.Lerp(startPos, finalPos, easeT);
            yield return null;
        }
        
        targetRect.anchoredPosition = finalPos;
    }

    // --- UPDATED: Completely revamped StartNextDay ---
    public void StartNextDay()
    {
        AudioManager.Instance.Play("ButtonPop");
        
        if (nightHubPanel != null) nightHubPanel.SetActive(false);
        DayTimeUI.SetActive(true);

        IceCreamStack.Instance.ResetStack(); 
        
        // Start the day immediately, but pass FALSE so it doesn't instantly snap the lights!
        StartDay(false); 

        // Trigger the beautiful morning sunrise transitions!
        StartCoroutine(FadeLightingToDay());
        StartCoroutine(GlideCameraToDay());
    }

    // --- NEW: Sunrise Lighting Coroutine ---
    private IEnumerator FadeLightingToDay()
    {
        if (dayLight == null || nightLight == null) yield break;

        Light dLight = dayLight.GetComponent<Light>();
        Light nLight = nightLight.GetComponent<Light>();

        dayLight.SetActive(true);
        nightLight.SetActive(true);
        dLight.intensity = 0f; 

        // Swap to the beautiful morning skybox immediately
        if (daySkybox != null) RenderSettings.skybox = daySkybox;

        float elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / cameraTransitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            // Moon goes down, Sun comes up!
            dLight.intensity = Mathf.Lerp(0f, originalDayIntensity, easeT);
            nLight.intensity = Mathf.Lerp(originalNightIntensity, 0f, easeT);

            yield return null;
        }

        dLight.intensity = originalDayIntensity;
        nLight.intensity = 0f;
        nightLight.SetActive(false);
    }

    // --- NEW: Glide Camera Back Coroutine ---
    private IEnumerator GlideCameraToDay()
    {
        if (gameplayCamera == null || nightCameraTarget == null) yield break;

        Vector3 startPos = gameplayCamera.transform.position;
        Quaternion startRot = gameplayCamera.transform.rotation;
        float startFOV = gameplayCamera.fieldOfView;

        float elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / cameraTransitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            gameplayCamera.transform.position = Vector3.Lerp(startPos, originalCamPos, easeT);
            gameplayCamera.transform.rotation = Quaternion.Lerp(startRot, originalCamRot, easeT);
            gameplayCamera.fieldOfView = Mathf.Lerp(startFOV, originalCamFOV, easeT);

            yield return null;
        }

        gameplayCamera.transform.position = originalCamPos;
        gameplayCamera.transform.rotation = originalCamRot;
        gameplayCamera.fieldOfView = originalCamFOV;
        
        if (CameraSwipeMover.Instance != null) 
            CameraSwipeMover.Instance.enabled = true;
    }

    private void SaveDay()
    {
        PlayerPrefs.SetInt("CurrentDay", currentDay);
        PlayerPrefs.Save();
    }

    private void LoadDay()
    {
        currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
    }
}