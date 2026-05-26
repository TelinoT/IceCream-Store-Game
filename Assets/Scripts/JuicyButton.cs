using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for detecting touches
using System.Collections;

public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Settings")]
    public float pressScale = 0.9f;   // How small it gets (0.9 = 90% size)
    public float duration = 0.1f;     // How fast it squishes
    
    [Header("Audio (Optional)")]
    public AudioClip clickSound;
    private AudioSource audioSource;

    private Vector3 originalScale;
    private Coroutine currentRoutine;

    void Awake()
    {
        // Remember the size you set in the editor (in case it's not 1,1,1)
        originalScale = transform.localScale;

        // Auto-find or add AudioSource if sound is needed
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void OnEnable()
    {
        // Reset whenever the button appears (fixes ScrollView bugs)
        transform.localScale = originalScale;
    }

    // 1. PRESSED DOWN
    public void OnPointerDown(PointerEventData eventData)
    {
        // Play Sound
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);

        // Animate to Small
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(TweenScale(originalScale * pressScale));
    }

    // 2. RELEASED (FINGER LIFTED)
    public void OnPointerUp(PointerEventData eventData)
    {
        // Animate back to Normal with a slight bounce
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(BounceBack());
    }

    // 3. DRAGGED OFF (If user slides finger off button)
    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset gently without the bounce
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(TweenScale(originalScale));
    }

    // Simple smooth movement
    IEnumerator TweenScale(Vector3 targetScale)
    {
        float timer = 0f;
        Vector3 startScale = transform.localScale;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled so it works in Pause Menus!
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    // The "Over-shoot" bounce effect
    IEnumerator BounceBack()
    {
        // Phase 1: Go slightly bigger than normal (1.1x)
        float timer = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 overshootScale = originalScale * 1.05f; // 5% bigger

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(startScale, overshootScale, t);
            yield return null;
        }

        // Phase 2: Settle back to normal
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(overshootScale, originalScale, t);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}