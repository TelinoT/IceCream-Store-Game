using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TaskUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tasksPanel;
    public RectTransform tasksPanelRect;
    
    [Header("Containers")]
    public Transform todayTasksContent;
    public Transform tomorrowTasksContent;
    public Transform generalTasksContent;

    [Header("Prefabs")]
    public GameObject taskUIPrefab;

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public Vector2 offScreenBottom = new Vector2(0f, -1500f);

    private Vector2 originalUIPosition;
    private Coroutine currentAnim;
    private bool isAnimating = false;

    void Start()
    {
        if (tasksPanelRect == null && tasksPanel != null)
        {
            tasksPanelRect = tasksPanel.GetComponent<RectTransform>();
        }

        if (tasksPanelRect != null)
        {
            originalUIPosition = tasksPanelRect.anchoredPosition;
            tasksPanelRect.anchoredPosition = offScreenBottom;
        }

        if (tasksPanel != null) tasksPanel.SetActive(false);
    }

    public void OpenTasksMenu()
    {
        if (isAnimating) return;
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.HideUI();

        if (AudioManager.Instance != null) AudioManager.Instance.Play("ButtonPop");
        
        tasksPanel.SetActive(true);
        PopulateAllTasks();

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideIn());
    }

    public void CloseTasksMenu()
    {
        if (isAnimating) return;
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.ShowUI();

        if (AudioManager.Instance != null) AudioManager.Instance.Play("ButtonPop");

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideOut());
    }

    private void PopulateAllTasks()
    {
        PopulateList(TaskManager.Instance.todayTasks, todayTasksContent, false);
        PopulateList(TaskManager.Instance.tomorrowTasks, tomorrowTasksContent, true);
        PopulateList(TaskManager.Instance.currentLifetimeTasks, generalTasksContent, false);
    }

    private void PopulateList(List<ActiveTask> taskList, Transform container, bool isPreview)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        foreach (ActiveTask task in taskList)
        {
            GameObject taskObj = Instantiate(taskUIPrefab, container);
            TaskUIItem uiItem = taskObj.GetComponent<TaskUIItem>();
            
            if (uiItem != null)
            {
                uiItem.Setup(task, this, isPreview);
            }
        }
    }

    private IEnumerator SlideIn()
    {
        isAnimating = true;
        tasksPanelRect.anchoredPosition = offScreenBottom;
        
        if (DayManager.Instance != null)
        {
            DayManager.Instance.CloseNightHubPanel();
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / slideDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            tasksPanelRect.anchoredPosition = Vector2.Lerp(offScreenBottom, originalUIPosition, easeT);
            yield return null;
        }

        tasksPanelRect.anchoredPosition = originalUIPosition;
        isAnimating = false;
    }

    private IEnumerator SlideOut()
    {
        isAnimating = true;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / slideDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            tasksPanelRect.anchoredPosition = Vector2.Lerp(originalUIPosition, offScreenBottom, easeT);
            yield return null;
        }

        tasksPanelRect.anchoredPosition = offScreenBottom;
        tasksPanel.SetActive(false);
        isAnimating = false;
        
        // Return to Night Hub
        if (DayManager.Instance != null)
        {
            DayManager.Instance.ShowNightHubPanel();
        }
    }
}