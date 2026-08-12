using UnityEngine;
using System.Collections.Generic;

public class VIPManager : MonoBehaviour
{
    public static VIPManager Instance;
    
    [Header("VIP Roster")]
    public List<VIPCharacterData> allVIPs;
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float vipSpawnChance = 0.3f;
    private bool vipSpawnedToday = false;

    void Awake()
    {
        Instance = this;
    }

    public void ResetDailyVIPLimit()
    {
        vipSpawnedToday = false;
    }

    public VIPCharacterData GetReadyVIP()
    {
        // Don't overwhelm the player: only one VIP per day
        if (vipSpawnedToday || Random.value > vipSpawnChance) return null;

        foreach (var vip in allVIPs)
        {
            int encounterIndex = PlayerPrefs.GetInt("VIP_Encounter_" + vip.characterName, 0);
            int waiting = PlayerPrefs.GetInt("VIP_WaitingForTask_" + vip.characterName, 0);

            // Story completely finished for this VIP!
            if (encounterIndex >= vip.encounters.Count) continue; 

            VIPEncounter currentEncounter = vip.encounters[encounterIndex];

            if (waiting == 1)
            {
                // Check if the task is still sitting in the active lifetime task list
                bool taskStillActive = TaskManager.Instance.currentLifetimeTasks.Exists(t => t.data == currentEncounter.taskToAssign);
                
                // If it's no longer in the list, the player finished and claimed it! Move to next chapter.
                if (!taskStillActive)
                {
                    encounterIndex++;
                    PlayerPrefs.SetInt("VIP_Encounter_" + vip.characterName, encounterIndex);
                    PlayerPrefs.SetInt("VIP_WaitingForTask_" + vip.characterName, 0);
                    PlayerPrefs.Save();

                    if (encounterIndex < vip.encounters.Count)
                    {
                        vipSpawnedToday = true;
                        return vip; 
                    }
                }
            }
            else
            {
                vipSpawnedToday = true;
                return vip; 
            }
        }
        return null;
    }

    public void AssignVIPTask(VIPCharacterData vip)
    {
        int encounterIndex = PlayerPrefs.GetInt("VIP_Encounter_" + vip.characterName, 0);
        VIPEncounter currentEncounter = vip.encounters[encounterIndex];

        if (currentEncounter.taskToAssign != null)
        {
            ActiveTask newTask = new ActiveTask(currentEncounter.taskToAssign);
            TaskManager.Instance.currentLifetimeTasks.Add(newTask);

            PlayerPrefs.SetInt("VIP_WaitingForTask_" + vip.characterName, 1);
            PlayerPrefs.Save();
        }
        else
        {
            // If they just wanted to chat with no task, prep the next encounter instantly
            PlayerPrefs.SetInt("VIP_Encounter_" + vip.characterName, encounterIndex + 1);
            PlayerPrefs.Save();
        }
    }
}