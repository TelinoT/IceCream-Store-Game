using UnityEngine;
using System.Collections;

public class PersistentUIController : MonoBehaviour
{
    public static PersistentUIController Instance;

    [Header("UI Reference")]
    [Tooltip("Drag the RectTransform of your persistent UI (like the Top Bar) here")]
    public RectTransform uiPanel; 

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    [Tooltip("Where it hides (e.g., Y = 150 to slide up off the top of the screen)")]
    public Vector2 hiddenPosition = new Vector2(0f, 150f);

    private Vector2 visiblePosition;
    private Coroutine currentAnim;

    void Awake()
    {
        if (Instance == null) Instance = this;

        if (uiPanel != null) 
        {
            // Memorize exactly where you placed it in the scene editor
            visiblePosition = uiPanel.anchoredPosition;
        }
    }

    public void HideUI()
    {
        if (uiPanel == null) return;
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideTo(hiddenPosition));
    }

    public void ShowUI()
    {
        if (uiPanel == null) return;
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideTo(visiblePosition));
    }

    private IEnumerator SlideTo(Vector2 targetPos)
    {
        float elapsed = 0f;
        Vector2 startPos = uiPanel.anchoredPosition;

        while (elapsed < slideDuration)
        {
            // Use unscaledDeltaTime so it animates perfectly even if time is paused!
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / slideDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            uiPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
            yield return null;
        }
        
        uiPanel.anchoredPosition = targetPos;
    }
}