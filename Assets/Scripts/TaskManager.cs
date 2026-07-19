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

    private List<TaskData> completedLifetimeTasks = new List<TaskData>();
    private const string SAVE_KEY = "TaskSaveData";

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
        if (!LoadTasks())
        {
            // First time playing (no save file found)
            InitializeLifetimeTasks();
            if (todayTasks.Count == 0) todayTasks = GetRandomTasks(dailyTasksPerDay);
            SaveTasks();
        }
        else
        {
            // --- NEW FAILSAFE ---
            // If the game loaded, but today's list is empty for any reason, fix it immediately!
            if (todayTasks.Count == 0)
            {
                todayTasks = GetRandomTasks(dailyTasksPerDay);
                SaveTasks();
            }
            // Make sure lifetime tasks are topped off
            if (currentLifetimeTasks.Count < maxActiveLifetimeTasks)
            {
                RefillLifetimeTasks();
                SaveTasks();
            }
        }
    }

    // --- CYCLE MANAGEMENT ---

    public void GenerateTomorrowTasks()
    {
        tomorrowTasks = GetRandomTasks(dailyTasksPerDay);
        SaveTasks(); 
    }

    public void RolloverToNewDay()
    {
        todayTasks.Clear();
        todayTasks.AddRange(tomorrowTasks);
        tomorrowTasks.Clear();

        for (int i = currentLifetimeTasks.Count - 1; i >= 0; i--)
        {
            if (currentLifetimeTasks[i].isRewardClaimed)
            {
                completedLifetimeTasks.Add(currentLifetimeTasks[i].data);
                currentLifetimeTasks.RemoveAt(i);
            }
        }

        RefillLifetimeTasks();
        SaveTasks(); 
    }

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

        for (int i = 0; i < availableTasks.Count; i++)
        {
            TaskData temp = availableTasks[i];
            int randomIndex = Random.Range(i, availableTasks.Count);
            availableTasks[i] = availableTasks[randomIndex];
            availableTasks[randomIndex] = temp;
        }

        for (int i = 0; i < Mathf.Min(neededSlots, availableTasks.Count); i++)
        {
            currentLifetimeTasks.Add(new ActiveTask(availableTasks[i]));
        }
    }

    // --- PROGRESS & REWARDS ---

    public void ReportProgress(TaskGoalType type, int amount)
    {
        bool progressMade = false;

        foreach (ActiveTask task in todayTasks)
        {
            if (task.data.goalType == type && !task.isCompleted)
            {
                // If AddProgress returns true, the task just finished!
                if (task.AddProgress(amount))
                {
                    if (TaskNotificationManager.Instance != null)
                        TaskNotificationManager.Instance.ShowNotification(task.data);
                }
                progressMade = true;
            }
        }

        foreach (ActiveTask task in currentLifetimeTasks)
        {
            if (task.data.goalType == type && !task.isCompleted)
            {
                if (task.AddProgress(amount))
                {
                    if (TaskNotificationManager.Instance != null)
                        TaskNotificationManager.Instance.ShowNotification(task.data);
                }
                progressMade = true;
            }
        }

        if (progressMade) SaveTasks(); 
    }

    public bool ClaimReward(ActiveTask task)
    {
        if (task.isCompleted && !task.isRewardClaimed)
        {
            task.isRewardClaimed = true;
            EconomyManager.Instance.AddReward(task.data.coinReward, task.data.xpReward, Vector3.zero);
            
            if (AudioManager.Instance != null) AudioManager.Instance.Play("BuyButton"); 
            
            SaveTasks(); 
            return true;
        }
        return false;
    }

    // ==========================================
    // --- SAVE & LOAD SYSTEM IMPLEMENTATION ---
    // ==========================================

    private void SaveTasks()
    {
        TaskSaveData saveData = new TaskSaveData();

        saveData.todayTasks = ConvertToSavedList(todayTasks);
        saveData.tomorrowTasks = ConvertToSavedList(tomorrowTasks);
        saveData.currentLifetimeTasks = ConvertToSavedList(currentLifetimeTasks);

        foreach (TaskData task in completedLifetimeTasks)
        {
            // Use file name as fallback if ID is blank
            string safeID = string.IsNullOrEmpty(task.taskID) ? task.name : task.taskID;
            saveData.completedLifetimeTaskIDs.Add(safeID);
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private bool LoadTasks()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return false; 

        string json = PlayerPrefs.GetString(SAVE_KEY);
        TaskSaveData saveData = JsonUtility.FromJson<TaskSaveData>(json);

        if (saveData == null) return false;

        todayTasks = ConvertToActiveList(saveData.todayTasks);
        tomorrowTasks = ConvertToActiveList(saveData.tomorrowTasks);
        currentLifetimeTasks = ConvertToActiveList(saveData.currentLifetimeTasks);

        completedLifetimeTasks.Clear();
        foreach (string id in saveData.completedLifetimeTaskIDs)
        {
            TaskData data = GetTaskDataByID(id);
            if (data != null) completedLifetimeTasks.Add(data);
        }

        return true; 
    }

    private List<SavedTask> ConvertToSavedList(List<ActiveTask> activeList)
    {
        List<SavedTask> savedList = new List<SavedTask>();
        foreach (ActiveTask active in activeList)
        {
            // --- THE FIX: Uses the ScriptableObject's actual file name if you forgot to set an ID ---
            string safeID = string.IsNullOrEmpty(active.data.taskID) ? active.data.name : active.data.taskID;

            savedList.Add(new SavedTask
            {
                taskID = safeID,
                currentProgress = active.currentProgress,
                isCompleted = active.isCompleted,
                isRewardClaimed = active.isRewardClaimed
            });
        }
        return savedList;
    }

    private List<ActiveTask> ConvertToActiveList(List<SavedTask> savedList)
    {
        List<ActiveTask> activeList = new List<ActiveTask>();
        foreach (SavedTask saved in savedList)
        {
            TaskData data = GetTaskDataByID(saved.taskID);
            if (data != null)
            {
                ActiveTask active = new ActiveTask(data);
                active.currentProgress = saved.currentProgress;
                active.isCompleted = saved.isCompleted;
                active.isRewardClaimed = saved.isRewardClaimed;
                activeList.Add(active);
            }
        }
        return activeList;
    }

    private TaskData GetTaskDataByID(string id)
    {
        foreach (TaskData data in allDailyTasksPool)
        {
            string safeID = string.IsNullOrEmpty(data.taskID) ? data.name : data.taskID;
            if (safeID == id) return data;
        }
        foreach (TaskData data in allLifetimeTasks)
        {
            string safeID = string.IsNullOrEmpty(data.taskID) ? data.name : data.taskID;
            if (safeID == id) return data;
        }
        Debug.LogWarning($"TaskData with ID '{id}' not found! Ensure it is in the Manager's pools.");
        return null;
    }
}

// ==========================================
// --- DATA CLASSES FOR SERIALIZATION ---
// ==========================================

[System.Serializable]
public class SavedTask
{
    public string taskID;
    public int currentProgress;
    public bool isCompleted;
    public bool isRewardClaimed;
}

[System.Serializable]
public class TaskSaveData
{
    public List<SavedTask> todayTasks = new List<SavedTask>();
    public List<SavedTask> tomorrowTasks = new List<SavedTask>();
    public List<SavedTask> currentLifetimeTasks = new List<SavedTask>();
    public List<string> completedLifetimeTaskIDs = new List<string>();
}