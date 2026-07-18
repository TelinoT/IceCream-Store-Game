using UnityEngine;

public enum TaskType { Daily, Lifetime }
public enum TaskGoalType 
{ 
    ServeCustomers, 
    EarnMoney, 
    SellPerfectIceCream, 
    AddSprinkles, 
    ServeCones, 
    BuyUpgrades 
}
[CreateAssetMenu(fileName = "NewTask", menuName = "Shop/TaskData")]
public class TaskData : ScriptableObject
{
    [Header("Settings")]
    public TaskType taskType;
    public TaskGoalType goalType;
    
    [Tooltip("How many times does the action need to be done?")]
    public int targetAmount = 10;

    [Header("Rewards")]
    public int coinReward = 50;
    public int xpReward = 10; 
}