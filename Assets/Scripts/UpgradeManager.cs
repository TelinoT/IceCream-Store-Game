using UnityEngine;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("All Available Upgrades")]
    public List<UpgradeData> allUpgrades;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public int GetUpgradeLevel(string upgradeID)
    {
        return PlayerPrefs.GetInt("Upgrade_" + upgradeID, 0); 
    }

    public int GetNextLevelCost(UpgradeData upgrade)
    {
        int currentLevel = GetUpgradeLevel(upgrade.upgradeID);
        if (currentLevel >= upgrade.maxLevel) return 999999; 

        // Calculates exponential cost: Base * (Multiplier ^ Level)
        float calculatedCost = upgrade.baseCost * Mathf.Pow(upgrade.costMultiplier, currentLevel);
        return Mathf.RoundToInt(calculatedCost);
    }

    public float GetCurrentStatValueByID(string upgradeID)
    {
        UpgradeData data = allUpgrades.Find(u => u.upgradeID == upgradeID);
        if (data != null)
        {
            // CHANGED: We use "data.upgradeID" instead of "upgrade.upgradeID"
            int currentLevel = GetUpgradeLevel(data.upgradeID); 
            return data.baseStatValue + (data.statIncreasePerLevel * currentLevel);
        }
        
        Debug.LogWarning("Upgrade ID not found: " + upgradeID);
        return 0f;
    }

    public bool TryBuyUpgrade(UpgradeData upgrade)
    {
        int currentLevel = GetUpgradeLevel(upgrade.upgradeID);
        if (currentLevel >= upgrade.maxLevel) return false;

        int cost = GetNextLevelCost(upgrade);

        if (EconomyManager.Instance.TrySpendCoins(cost))
        {
            PlayerPrefs.SetInt("Upgrade_" + upgrade.upgradeID, currentLevel + 1);
            PlayerPrefs.Save();
            AudioManager.Instance.Play("BuyButton");
            TaskManager.Instance.ReportProgress(TaskGoalType.BuyUpgrades, 1);
            return true;
        }

        return false; 
    }
}