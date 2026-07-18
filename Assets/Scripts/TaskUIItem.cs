using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskUIItem : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text goalText; 
    public TMP_Text progressText;
    public TMP_Text rewardText;
    public Button claimButton;
    public TMP_Text claimButtonText;

    private ActiveTask currentTask;
    private TaskUIManager uiManager;

    public void Setup(ActiveTask task, TaskUIManager manager, bool isPreview)
    {
        currentTask = task;
        uiManager = manager;

        // Automatically format the enum into a readable sentence
        goalText.text = FormatGoalString(task.data.goalType);
        
        rewardText.text = $"{task.data.coinReward} $$"; 

        UpdateUI(isPreview);

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClaimClicked);
    }

    private void UpdateUI(bool isPreview)
    {
        progressText.text = $"{currentTask.currentProgress} / {currentTask.data.targetAmount}";

        if (isPreview)
        {
            claimButton.interactable = false;
            claimButtonText.text = "TOMORROW";
        }
        else if (currentTask.isRewardClaimed)
        {
            claimButton.interactable = false;
            claimButtonText.text = "CLAIMED";
        }
        else if (currentTask.isCompleted)
        {
            claimButton.interactable = true;
            claimButtonText.text = "CLAIM";
        }
        else
        {
            claimButton.interactable = false;
            
            if (currentTask.data.taskType == TaskType.Daily)
            {
                claimButtonText.text = "FAILED";
            }
            else
            {
                claimButtonText.text = "IN PROGRESS";
            }
        }
    }

    private void OnClaimClicked()
    {
        if (TaskManager.Instance.ClaimReward(currentTask))
        {
            UpdateUI(false);
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play("Fail");
        }
    }

    // Helper method to make the GoalType enum look nice in the UI
    private string FormatGoalString(TaskGoalType type)
    {
        switch(type)
        {
            case TaskGoalType.ServeCustomers: return "Serve Customers";
            case TaskGoalType.EarnMoney: return "Earn Money";
            case TaskGoalType.SellPerfectIceCream: return "Perfect Ice Creams";
            case TaskGoalType.AddSprinkles: return "Add Sprinkles";
            case TaskGoalType.ServeCones: return "Serve Cones";
            case TaskGoalType.BuyUpgrades: return "Buy Upgrades";
            default: return type.ToString();
        }
    }
}