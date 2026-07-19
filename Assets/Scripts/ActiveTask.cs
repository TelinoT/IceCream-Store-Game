using UnityEngine;

[System.Serializable]
public class ActiveTask
{
    public TaskData data;
    public int currentProgress;
    public bool isCompleted;
    public bool isRewardClaimed;

    public ActiveTask(TaskData taskData)
    {
        data = taskData;
        currentProgress = 0;
        isCompleted = false;
        isRewardClaimed = false;
    }

    // --- CHANGED: Now returns true if the task was JUST completed ---
    public bool AddProgress(int amount)
    {
        if (isCompleted) return false;

        currentProgress += amount;

        if (currentProgress >= data.targetAmount)
        {
            currentProgress = data.targetAmount;
            isCompleted = true;
            return true; // We just finished it!
        }
        
        return false; // Still working on it
    }
}