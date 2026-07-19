using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TaskNotificationManager : MonoBehaviour
{
    public static TaskNotificationManager Instance;

    [Header("UI References")]
    public RectTransform bannerRect;
    public TMP_Text taskNameText;

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public float displayDuration = 2.5f;
    
    [Tooltip("Where it hides (e.g., Far right, off the screen)")]
    public Vector2 offScreenPos = new Vector2(800f, 0f); 
    
    [Tooltip("Where it rests on screen (e.g., Just inside the right edge)")]
    public Vector2 onScreenPos = new Vector2(-20f, 0f);

    private Queue<TaskData> notificationQueue = new Queue<TaskData>();
    private bool isAnimating = false;

    void Awake()
    {
        Instance = this;
        if (bannerRect != null) bannerRect.anchoredPosition = offScreenPos;
    }

    public void ShowNotification(TaskData task)
    {
        notificationQueue.Enqueue(task);
        
        if (!isAnimating)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isAnimating = true;

        while (notificationQueue.Count > 0)
        {
            TaskData task = notificationQueue.Dequeue();
            
            if (taskNameText != null) 
            {
                taskNameText.text = "Task Complete:\n" + FormatGoalString(task.goalType);
            }
            
            if (AudioManager.Instance != null) AudioManager.Instance.Play("ButtonPop");

            // 1. Slide In
            yield return StartCoroutine(SlideBanner(offScreenPos, onScreenPos));

            // 2. Wait
            yield return new WaitForSeconds(displayDuration);

            // 3. Slide Out
            yield return StartCoroutine(SlideBanner(onScreenPos, offScreenPos));
            
            yield return new WaitForSeconds(0.2f);
        }

        isAnimating = false;
    }

    private IEnumerator SlideBanner(Vector2 start, Vector2 end)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime; 
            float t = elapsed / slideDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            bannerRect.anchoredPosition = Vector2.Lerp(start, end, easeT);
            yield return null;
        }
        
        bannerRect.anchoredPosition = end;
    }

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