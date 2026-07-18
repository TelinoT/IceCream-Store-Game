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

    public void AddProgress(int amount)
    {
        if (isCompleted) return;

        currentProgress += amount;

        if (currentProgress >= data.targetAmount)
        {
            currentProgress = data.targetAmount;
            isCompleted = true;
            
            // Optional: Play a tiny pop sound when a task completes mid-day!
            // if (AudioManager.Instance != null) AudioManager.Instance.Play("TaskComplete");
        }
    }
}