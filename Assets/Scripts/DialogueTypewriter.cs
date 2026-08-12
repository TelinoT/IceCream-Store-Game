using UnityEngine;
using TMPro; // Required for TextMeshPro!
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DialogueTypewriter : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    [Header("Typewriter Settings")]
    public float typeSpeed = 0.04f;      // How fast normal letters appear
    public float punctuationDelay = 0.2f;// How long it pauses at periods/commas
    
    [Header("Audio")]
    public string blipSoundName = "TalkBlip"; // The sound to play from your AudioManager
    [Range(1, 4)]
    public int blipFrequency = 2; // Plays a sound every X letters (so it's not too annoying)

    private Coroutine typingCoroutine;
    
    public bool isTyping = false;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    // Call this from your CustomerManager when they give their order!
    public void ShowDialogue(string newText)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeRoutine(newText));
    }

    // Call this if the player clicks the screen to skip the animation
    public void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        textMesh.maxVisibleCharacters = textMesh.text.Length;
        
        isTyping = false;
    }

    private IEnumerator TypeRoutine(string textToType)
    {
        isTyping = true;
        
        // Set the text, but hide all the characters initially
        textMesh.text = textToType;
        textMesh.maxVisibleCharacters = 0;

        int totalVisibleCharacters = textToType.Length;
        int visibleCount = 0;

        while (visibleCount <= totalVisibleCharacters)
        {
            textMesh.maxVisibleCharacters = visibleCount;

            // Play the blip sound, but skip spaces so it feels rhythmic!
            if (visibleCount > 0 && visibleCount % blipFrequency == 0)
            {
                char lastChar = textToType[visibleCount - 1];
                if (lastChar != ' ')
                {
                    AudioManager.Instance.Play(blipSoundName);
                }
            }

            // Check if we just typed a punctuation mark to add a dramatic pause
            if (visibleCount > 0 && visibleCount < totalVisibleCharacters)
            {
                char lastChar = textToType[visibleCount - 1];
                if (lastChar == '.' || lastChar == ',' || lastChar == '!' || lastChar == '?')
                {
                    yield return new WaitForSeconds(punctuationDelay);
                }
                else
                {
                    yield return new WaitForSeconds(typeSpeed);
                }
            }
            else
            {
                yield return new WaitForSeconds(typeSpeed);
            }

            visibleCount++;
        }
        
        isTyping = false;
    }
}