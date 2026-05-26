using UnityEngine;
using TMPro; // Needed for TextMeshPro!

public class CustomerIdentity : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Drag the TextMeshPro element for the Name Tag here")]
    public TextMeshProUGUI nameText; 

    [Header("Name Pool")]
    [Tooltip("Add as many names as you want here!")]
    public string[] possibleNames = {
        "Oliver", "Emma", "Liam", "Mia", "Noah", 
        "Ava", "Elijah", "Sophia", "Lucas", "Isabella",
        "Mateo", "Amelia", "Leo", "Harper", "Finn",
        "Eni", "Felix", "Clara", "Julian", "Giulia"
    };

    private void Start()
    {
        AssignRandomName();
    }

    private void AssignRandomName()
    {
        // Make sure we actually assigned a text field in the inspector
        if (nameText != null && possibleNames.Length > 0)
        {
            // Pick a random number between 0 and the length of our list
            int randomIndex = Random.Range(0, possibleNames.Length);
            
            // Apply that random name to the UI text
            nameText.text = possibleNames[randomIndex];
        }
        else
        {
            Debug.LogWarning("CustomerIdentity: Missing nameText reference or empty name list on " + gameObject.name);
        }
    }
}