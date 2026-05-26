using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Shop/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    [Header("Basic Info")]
    public string upgradeID; 
    public string displayName;
    [TextArea] public string description;
    
    public Sprite icon; 

    [Header("Leveling & Costs")]
    public int maxLevel = 5;
    public int baseCost = 100; 
    public float costMultiplier = 1.5f;

    [Header("Stat Changes")]
    public float baseStatValue; 
    public float statIncreasePerLevel; 
}