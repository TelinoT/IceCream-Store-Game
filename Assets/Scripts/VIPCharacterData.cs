using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class VIPEncounter
{
    [Header("Story (Plays Sequentially)")]
    [Tooltip("The VIP will say these lines one by one, with a short pause between them.")]
    [TextArea(2, 4)]
    public List<string> introLines; 

    [Header("Player Choices")]
    [Tooltip("Leave Option A blank if you just want them to talk without a choice.")]
    public string optionA_Text;
    public string optionB_Text;

    [Header("VIP Responses")]
    [Tooltip("What the VIP says if you click Option A (plays sequentially).")]
    [TextArea(2, 4)]
    public List<string> responseA_Lines; 
    
    [Tooltip("What the VIP says if you click Option B.")]
    [TextArea(2, 4)]
    public List<string> responseB_Lines;

    [Header("Gameplay Mechanics")]
    [Tooltip("If false, the VIP just chats, has infinite patience, and leaves without ordering.")]
    public bool requiresOrder = true;
    
    [Tooltip("Leave blank for a dynamic order, or force a specific recipe for story reasons.")]
    public IceCreamRecipe specificOrder; 
    
    [Tooltip("The lifetime task assigned to the player after this conversation finishes.")]
    public TaskData taskToAssign; 
}

[CreateAssetMenu(fileName = "NewVIP", menuName = "Shop/VIP Character")]
public class VIPCharacterData : ScriptableObject
{
    public string characterName;
    
    [Header("3D Model")]
    [Tooltip("The specific 3D prefab to spawn for this character.")]
    public GameObject vipPrefab;
    
    [Header("The Storyline")]
    [Tooltip("Encounter 0 happens first. Once its task is done, Encounter 1 happens next time they visit.")]
    public List<VIPEncounter> encounters;
}