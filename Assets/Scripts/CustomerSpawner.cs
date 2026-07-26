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

    public string[] toppingTemplates = new string[] {
        " Oh, and add {2} on top!",
        " With some {2} please.",
        " And throw some {2} on there."
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

        IceCreamIngredient chosenBase = availableBases[Random.Range(0, availableBases.Count)];
        newRecipe.baseCone = chosenBase;
        int totalPrice = chosenBase.price;

        int scoopCount = 1;

        if (forceScoopCount > 0)
        {
            scoopCount = forceScoopCount; // Use the debug override
        }
        else
        {
            float roll = Random.value; // Rolls a random number between 0.0 and 1.0

            if (roll <= chanceOneScoop)
            {
                scoopCount = 1;
            }
            else if (roll <= chanceOneScoop + chanceTwoScoops)
            {
                scoopCount = 2;
            }
            else
            {
                scoopCount = 3;
            }
        }
        
        scoopCount = Mathf.Clamp(scoopCount, 1, 3);
        
        List<string> flavorNames = new List<string>();

        for (int i = 0; i < scoopCount; i++)
        {
            IceCreamIngredient chosenFlavor = unlockedFlavors[Random.Range(0, unlockedFlavors.Count)];
            newRecipe.flavors.Add(chosenFlavor);
            totalPrice += chosenFlavor.price;
            flavorNames.Add(chosenFlavor.ingredientName);
        }

        IceCreamIngredient chosenTopping = null;
        bool wantsTopping = Random.value > 0.65f && unlockedToppings.Count > 0;
        if (wantsTopping)
        {
            chosenTopping = unlockedToppings[Random.Range(0, unlockedToppings.Count)];
            newRecipe.toppings.Add(chosenTopping);
            totalPrice += chosenTopping.price;
        }
        
        newRecipe.price = totalPrice;

        string flavorText = "";
        if (scoopCount == 1) flavorText = flavorNames[0];
        else if (scoopCount == 2) flavorText = $"{flavorNames[0]} and {flavorNames[1]}";
        else if (scoopCount == 3) flavorText = $"{flavorNames[0]}, {flavorNames[1]}, and {flavorNames[2]}";

        string dialogue = baseAndFlavorTemplates[Random.Range(0, baseAndFlavorTemplates.Length)];
        
        if (wantsTopping)
        {
            dialogue += toppingTemplates[Random.Range(0, toppingTemplates.Length)];
            dialogue = string.Format(dialogue, chosenBase.ingredientName, flavorText, chosenTopping.ingredientName);
            // {0} = Base, {1} = Flavors, {2} = Topping
        }
        else
        {
            // {0} = Base, {1} = Flavors, {2} is ignored
            dialogue = string.Format(dialogue, chosenBase.ingredientName, flavorText, "");
        }

        newRecipe.orderLines = new string[] { dialogue };
        newRecipe.correctResponseLines = new string[] { "Yum! Thanks!", "Perfect!", "Looks delicious!", "Perfect! Just what I wanted!", "Yay! That looks delicious!", "Mmm… thank you so much!" };
        newRecipe.wrongResponseLines = new string[] { "Uh... this isn't what I ordered.", "I think you messed up.", "Oops… that’s not what I ordered.", "Close, but not quite." };

        return newRecipe;
    }
}