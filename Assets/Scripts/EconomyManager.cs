using UnityEngine;
using TMPro;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    public int coins = 0;
    public int xp = 0;

    public TextMeshProUGUI coinText;

    private const string CoinsKey = "Coins";
    private const string XPKey = "XP";
    
    [Header("Juice Settings")]
    public GameObject floatingTextPrefab; // Drag your FloatingHeartPrefab here!
    public string[] successEmojis = new string[] { "Hello" }; // Default emojis
    
    [Header("Position Randomness")]
    public float radius = 0.6f; // How wide they can spread out
    public float baseHeight = 2.0f; // How high above the customer they start

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadEconomy();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        UpdateCoinText();
    }

    void Update()
    {
        /*if (coinText != null)
            coinText.text = PlayerPrefs.GetInt("Coins", 0) + " $$";
            */

        if (Input.GetKey(KeyCode.M))
        {
            coins += 1;
            SaveEconomy();
            UpdateCoinText();
        }
    }
    
    private int displayedCoins = 0;
    
    private void UpdateCoinText()
    {
        // Start the animation routine
        StopAllCoroutines();
        StartCoroutine(AnimateCoinCount());
    }

    // Add this new Coroutine
    private System.Collections.IEnumerator AnimateCoinCount()
    {
        float duration = 0.5f; // Faster is usually better for UI
        float elapsed = 0f;
    
        // Ensure we start from the current visual value
        // (This fixes the "jumping" issue if you click buy rapidly)
        float startValue = displayedCoins; 

        while (elapsed < duration)
        {
            // FIX: Use unscaledDeltaTime so it works while Paused (Time.timeScale = 0)
            elapsed += Time.unscaledDeltaTime; 
        
            // Calculate progress (0 to 1)
            float t = elapsed / duration;
        
            // Optional: Add 'SmoothStep' for a nicer curve
            // t = t * t * (3f - 2f * t); 

            displayedCoins = (int)Mathf.Lerp(startValue, coins, t);
    
            if (coinText != null) 
                coinText.text = displayedCoins + " $$";
        
            // FIX: Wait for the next "Real Time" frame, not "Game Time" frame
            yield return null; 
        }

        // Force exact value at the end
        displayedCoins = coins;
        if (coinText != null) coinText.text = displayedCoins + " $$";
    }

    public void AddReward(int coinAmount, int xpAmount, Vector3 popupPosition)
    {
        coins = PlayerPrefs.GetInt("Coins", 0) ;
        
        coins += coinAmount;
        xp += xpAmount;
        
        TaskManager.Instance.ReportProgress(TaskGoalType.EarnMoney, coinAmount);
        
        if (LevelManager.Instance != null) LevelManager.Instance.CalculateLevel();

        SaveEconomy();

        //Debug.Log($"💰 +{coinAmount} coins | ⭐ +{xpAmount} XP");
        UpdateCoinText();
        
        if (floatingTextPrefab != null)
        {
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            
            randomOffset.y += baseHeight;
            
            // Spawn slightly above the customer's head (Vector3.up * 2.0f)
            Vector3 spawnPos = popupPosition + randomOffset;
            GameObject popup = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            
            // Pick a random emoji from the list
            string randomEmoji = "❤️"; 
            if (successEmojis.Length > 0)
            {
                randomEmoji = successEmojis[Random.Range(0, successEmojis.Length)];
            }

            // Set the text on the prefab
            // We use a pinkish color (1, 0.4, 0.7) for a cute look
            var floatingScript = popup.GetComponent<FloatingText>();
            if (floatingScript != null)
            {
                floatingScript.SetText(randomEmoji, new Color(1f, 0.75f, 0.2f));
            }
        }
    }
    
    public bool TrySpendCoins(int price)
    {
        if (coins >= price)
        {
            coins -= price;
            SaveEconomy(); // Auto-saves and updates the text animation!
            return true; // Purchase success
        }
        return false; // Not enough money
    }

    private void SaveEconomy()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.SetInt(XPKey, xp);
        PlayerPrefs.Save();
        UpdateCoinText();
    }

    public void LoadEconomy()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0); // 0 as default
        xp = PlayerPrefs.GetInt(XPKey, 0);
        UpdateCoinText();
    }
}