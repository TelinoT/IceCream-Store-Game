using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    [Header("Task Pools")]
    public List<TaskData> allDailyTasksPool;
    public List<TaskData> allLifetimeTasks;

    [Header("Active Tasks")]
    public List<ActiveTask> todayTasks = new List<ActiveTask>();
    public List<ActiveTask> tomorrowTasks = new List<ActiveTask>();
    public List<ActiveTask> currentLifetimeTasks = new List<ActiveTask>();

    [Header("Limits & Settings")]
    public int dailyTasksPerDay = 3;
    public int maxActiveLifetimeTasks = 3;

    // We use this to remember which lifetime tasks are completely done so we don't redraw them
    private List<TaskData> completedLifetimeTasks = new List<TaskData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeLifetimeTasks();
        
        // If starting the game for the very first time, generate today's tasks
        if (todayTasks.Count == 0)
        {
            todayTasks = GetRandomTasks(dailyTasksPerDay);
        }
    }

    // --- CYCLE MANAGEMENT ---

    public void GenerateTomorrowTasks()
    {
        tomorrowTasks = GetRandomTasks(dailyTasksPerDay);
    }

    public void RolloverToNewDay()
    {
        // 1. Rollover Daily Tasks
        todayTasks.Clear();
        todayTasks.AddRange(tomorrowTasks);
        tomorrowTasks.Clear();

        // 2. Clean up claimed Lifetime Tasks
        for (int i = currentLifetimeTasks.Count - 1; i >= 0; i--)
        {
            if (currentLifetimeTasks[i].isRewardClaimed)
            {
                // Mark as permanently completed and remove from active list
                completedLifetimeTasks.Add(currentLifetimeTasks[i].data);
                currentLifetimeTasks.RemoveAt(i);
            }
        }

        // 3. Draw fresh Lifetime Tasks to fill any empty slots
        RefillLifetimeTasks();
    }

    // Helper method to grab random DAILY tasks from the pool
    private List<ActiveTask> GetRandomTasks(int amount)
    {
        List<ActiveTask> newTasks = new List<ActiveTask>();
        List<TaskData> shuffledPool = new List<TaskData>(allDailyTasksPool);
        
        for (int i = 0; i < shuffledPool.Count; i++)
        {
            TaskData temp = shuffledPool[i];
            int randomIndex = Random.Range(i, shuffledPool.Count);
            shuffledPool[i] = shuffledPool[randomIndex];
            shuffledPool[randomIndex] = temp;
        }

        for (int i = 0; i < Mathf.Min(amount, shuffledPool.Count); i++)
        {
            newTasks.Add(new ActiveTask(shuffledPool[i]));
        }

        return newTasks;
    }

    // --- LIFETIME TASK MANAGEMENT ---

    private void InitializeLifetimeTasks()
    {
        currentLifetimeTasks.Clear();
        RefillLifetimeTasks();
    }

    private void RefillLifetimeTasks()
    {
        int neededSlots = maxActiveLifetimeTasks - currentLifetimeTasks.Count;
        if (neededSlots <= 0) return;

        // Gather all lifetime tasks that are NOT currently active and NOT completely finished
        List<TaskData> availableTasks = new List<TaskData>();
        foreach (TaskData task in allLifetimeTasks)
        {
            bool isActive = currentLifetimeTasks.Exists(t => t.data == task);
            bool isFinished = completedLifetimeTasks.Contains(task);

            if (!isActive && !isFinished)
            {
                availableTasks.Add(task);
            }
        }

        // Shuffle the available tasks
        for (int i = 0; i < availableTasks.Count; i++)
        {
            TaskData temp = availableTasks[i];
            int randomIndex = Random.Range(i, availableTasks.Count);
            availableTasks[i] = availableTasks[randomIndex];
            availableTasks[randomIndex] = temp;
        }

        // Add them until the limit is reached
        for (int i = 0; i < Mathf.Min(neededSlots, availableTasks.Count); i++)
        {
            currentLifetimeTasks.Add(new ActiveTask(availableTasks[i]));
        }
    }

    // --- PROGRESS & REWARDS ---

    public void ReportProgress(TaskGoalType type, int amount)
    {
        foreach (ActiveTask task in todayTasks)
        {
            if (task.data.goalType == type && !task.isCompleted)
            {
                task.AddProgress(amount);
            }
        }

        foreach (ActiveTask task in currentLifetimeTasks)
        {
            if (task.data.goalType == type && !task.isCompleted)
            {
                task.AddProgress(amount);
            }
        }
    }

    public bool ClaimReward(ActiveTask task)
    {
        if (task.isCompleted && !task.isRewardClaimed)
        {
            task.isRewardClaimed = true;
            EconomyManager.Instance.AddReward(task.data.coinReward, task.data.xpReward, Vector3.zero);
            
            if (AudioManager.Instance != null) AudioManager.Instance.Play("BuyButton"); 
            return true;
        }
        return false;
    }
}