using UnityEngine;

public enum UpgradeCategory { Perk, Flavor, Topping }

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Shop/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    [Header("Basic Info")]
    public string upgradeID; 
    public string displayName;
    [TextArea] public string description;
    
    public Sprite icon; 
    
    public UpgradeCategory category = UpgradeCategory.Perk; 
    public int requiredLevel = 1; 
    public bool isOneTimeUnlock = false; 

    [Header("Leveling & Costs")]
    public int maxLevel = 5;
    public int baseCost = 100; 
    public float costMultiplier = 1.5f;

    [Header("Stat Changes")]
    public float baseStatValue; 
    public float statIncreasePerLevel; 
}