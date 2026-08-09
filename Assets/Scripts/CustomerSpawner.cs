using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    
    [Header("Available Ingredients")]
    public List<IceCreamIngredient> availableBases;
    public List<IceCreamIngredient> availableFlavors;
    public List<IceCreamIngredient> availableToppings;

    [Header("Debug & Testing")]
    [Tooltip("Set to 0 for random scoops (1-3). Set to 1, 2, or 3 to force exact scoops for testing.")]
    public int forceScoopCount = 0;

    [Header("Dialogue Templates")]
    public string[] baseAndFlavorTemplates = new string[] {
        "I'd like a {0} with {1}, please!",
        "Can I get a {1} in a {0}?",
        "One {0} of {1}, thanks!",
        "I'm craving some {1}. Put it in a {0}!", 
        "{1} in a {0}, please."
    };

    public string[] toppingConnectors = new string[] {
        " And could you top that off with ",
        " Oh, and add ",
        " With ",
        " And throw on ",
        " Plus ",
        " And finish it with ",
        " Oh, and I also want "
    };
    
    [Range(0f, 1f)] public float chanceOneScoop = 0.6f;
    [Range(0f, 1f)] public float chanceTwoScoops = 0.3f;

    public void SpawnCustomer()
    {
        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        customer.transform.Rotate(0f, 90f, 0f);
        CustomerOrder order = customer.GetComponent<CustomerOrder>();
       
        order.desiredRecipe = GenerateDynamicRecipe();

        Buttons.Instance.currentCustomer = order;
        
        if (Buttons.Instance != null) 
        {
            Buttons.Instance.UpdateServeUI();
        }
        
        AudioManager.Instance.Play("Talking");
    }
    
    private IceCreamRecipe GenerateDynamicRecipe()
    {
        IceCreamRecipe newRecipe = ScriptableObject.CreateInstance<IceCreamRecipe>();
        newRecipe.flavors = new List<IceCreamIngredient>();
        newRecipe.toppings = new List<IceCreamIngredient>();
        
        List<IceCreamIngredient> unlockedFlavors = availableFlavors.FindAll(f => 
            string.IsNullOrEmpty(f.unlockID) || UpgradeManager.Instance.GetUpgradeLevel(f.unlockID) >= 1);
            
        List<IceCreamIngredient> unlockedToppings = availableToppings.FindAll(t => 
            string.IsNullOrEmpty(t.unlockID) || UpgradeManager.Instance.GetUpgradeLevel(t.unlockID) >= 1);

        if (unlockedFlavors.Count == 0) unlockedFlavors = availableFlavors;

        // --- BASE & FLAVORS ---
        IceCreamIngredient chosenBase = availableBases[Random.Range(0, availableBases.Count)];
        newRecipe.baseCone = chosenBase;
        int totalPrice = chosenBase.price;
        
        Debug.Log(totalPrice);

        int scoopCount = forceScoopCount > 0 ? forceScoopCount : 1;
        if (forceScoopCount == 0)
        {
            float roll = Random.value;
            if (roll <= chanceOneScoop) scoopCount = 1;
            else if (roll <= chanceOneScoop + chanceTwoScoops) scoopCount = 2;
            else scoopCount = 3;
        }
        scoopCount = Mathf.Clamp(scoopCount, 1, 3);
        
        List<string> flavorNames = new List<string>();
        for (int i = 0; i < scoopCount; i++)
        {
            IceCreamIngredient chosenFlavor = unlockedFlavors[Random.Range(0, unlockedFlavors.Count)];
            newRecipe.flavors.Add(chosenFlavor);
            totalPrice += chosenFlavor.price;
            Debug.Log(totalPrice);
            flavorNames.Add(chosenFlavor.ingredientName);
        }

        // --- NEW: WEIGHTED TOPPING MATH ---
        int toppingCount = 0;
        
        float topRoll = Random.value;
        
        float toppingCrazeBonus = UpgradeManager.Instance.GetCurrentStatValueByID("topping_craze") / 100f;
        topRoll += toppingCrazeBonus;
        
        if (unlockedToppings.Count > 0)
        {
            if (topRoll <= 0.35f) toppingCount = 0;      // Base 35% chance for 0
            else if (topRoll <= 0.75f) toppingCount = 1; // Base 40% chance for 1
            else if (topRoll <= 0.95f) toppingCount = 2; // Base 20% chance for 2
            else toppingCount = 3;                       // Base 5% chance for 3
        }
        
        // Cap it to how many unique toppings we actually have unlocked
        toppingCount = Mathf.Min(toppingCount, unlockedToppings.Count);

        // Shuffle the unlocked toppings so we don't pick the same one twice
        for (int i = 0; i < unlockedToppings.Count; i++)
        {
            IceCreamIngredient temp = unlockedToppings[i];
            int randomIndex = Random.Range(i, unlockedToppings.Count);
            unlockedToppings[i] = unlockedToppings[randomIndex];
            unlockedToppings[randomIndex] = temp;
        }

        // Add them to the recipe
        for (int i = 0; i < toppingCount; i++)
        {
            newRecipe.toppings.Add(unlockedToppings[i]);
            totalPrice += unlockedToppings[i].price;
            Debug.Log(totalPrice);
        }
        
        newRecipe.price = totalPrice;
        Debug.Log(totalPrice);

        string flavorText = "";
        if (scoopCount == 1) flavorText = flavorNames[0];
        else if (scoopCount == 2) flavorText = $"{flavorNames[0]} and {flavorNames[1]}";
        else if (scoopCount == 3) flavorText = $"{flavorNames[0]}, {flavorNames[1]}, and {flavorNames[2]}";

        string dialogue = baseAndFlavorTemplates[Random.Range(0, baseAndFlavorTemplates.Length)];
        dialogue = string.Format(dialogue, chosenBase.ingredientName, flavorText, "");

        if (toppingCount > 0)
        {
            string connector = toppingConnectors[Random.Range(0, toppingConnectors.Length)];
            string toppingDialogue = connector;
            
            if (toppingCount == 1)
            {
                toppingDialogue += $"{newRecipe.toppings[0].dialoguePrefix} {newRecipe.toppings[0].ingredientName}?";
            }
            else if (toppingCount == 2)
            {
                toppingDialogue += $"{newRecipe.toppings[0].dialoguePrefix} {newRecipe.toppings[0].ingredientName} and {newRecipe.toppings[1].dialoguePrefix} {newRecipe.toppings[1].ingredientName}?";
            }
            else if (toppingCount >= 3)
            {
                toppingDialogue += $"{newRecipe.toppings[0].dialoguePrefix} {newRecipe.toppings[0].ingredientName}, {newRecipe.toppings[1].dialoguePrefix} {newRecipe.toppings[1].ingredientName}, and {newRecipe.toppings[2].dialoguePrefix} {newRecipe.toppings[2].ingredientName}?";
            }
            
            dialogue += toppingDialogue;
        }

        newRecipe.orderLines = new string[] { dialogue };
        newRecipe.correctResponseLines = new string[] { "Yum! Thanks!", "Perfect!", "Looks delicious!" };
        newRecipe.wrongResponseLines = new string[] { "Uh... this isn't what I ordered.", "I think you messed up." };

        return newRecipe;
    }
}