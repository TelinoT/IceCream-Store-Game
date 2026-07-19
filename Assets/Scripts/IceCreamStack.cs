using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class IceCreamStack : MonoBehaviour
{
    public List<IceCreamIngredient> addedIngredients = new();
    public List<GameObject> addedObjects = new();
    
    [Header("Teleportation Anchors")]
    public Transform visualParent;
    public Transform frontCounterAnchor; 

    public static bool hasCone;
    
    private float currentHeight = 0f;
    
    private Vector3 originalBackPos;
    private Quaternion originalBackRot;
    
    public static IceCreamStack Instance;

    void Awake()
    {
        Instance = this;
        hasCone = false;
    }

    void Start()
    {
        // Memorize the exact spot the workstation parent starts at
        if (visualParent != null)
        {
            originalBackPos = visualParent.position;
            originalBackRot = visualParent.rotation;
        }
    }

    // --- NEW: Teleportation Logic ---
    public void MoveToCounter(bool toFront)
    {
        if (visualParent != null)
        {
            if (toFront && frontCounterAnchor != null)
            {
                visualParent.position = frontCounterAnchor.position;
                visualParent.rotation = frontCounterAnchor.rotation;
            }
            else
            {
                // Snap exactly back to where it was initially built
                visualParent.position = originalBackPos;
                visualParent.rotation = originalBackRot;
            }
        }
    }

    public bool HasBase() => addedIngredients.Any(i => i.type == IngredientType.Base);

    public void AddIngredient(IceCreamIngredient ingredient, GameObject visualObj)
    {
        if (ingredient.type == IngredientType.Base && HasBase()) return;
        if (ingredient.type != IngredientType.Base && !HasBase()) return;

        addedIngredients.Add(ingredient);
        addedObjects.Add(visualObj);
        
        if (Buttons.Instance != null) Buttons.Instance.UpdateServeUI();
    }

    public void AddIngredient(IceCreamIngredient ingredient)
    {
        if (ingredient.type != IngredientType.Base && !HasBase()) return;

        addedIngredients.Add(ingredient);

        if (ingredient.prefab != null && visualParent != null)
        {
            currentHeight += ingredient.stackHeight;

            GameObject obj = Instantiate(ingredient.prefab, visualParent);
            obj.transform.localPosition = new Vector3(0, currentHeight, 0);
            
            obj.transform.localRotation = Quaternion.Euler(
                Random.Range(-8f, 8f),   
                Random.Range(0f, 360f),  
                Random.Range(-8f, 8f)    
            );
        }
        
        if (Buttons.Instance != null) Buttons.Instance.UpdateServeUI();
    }

    public void AddSprinkles()
    {
        for (int i = 0; i < addedObjects.Count; i++)
        {
            if (addedIngredients[i].type == IngredientType.Base) continue;

            GameObject element = addedObjects[i];
            if (element.transform.childCount > 0)
            {
                Transform firstChild = element.transform.GetChild(0);
                firstChild.gameObject.SetActive(true);
            }
        }
    }

    public void ResetStack()
    {
        addedIngredients.Clear();
        addedObjects.Clear();
        currentHeight = 0f;

        foreach (Transform child in visualParent)
        {
            Destroy(child.gameObject);
        }
        
        hasCone = false;
    }

    public bool MatchesRecipe(IceCreamRecipe recipe)
    {
        if (!addedIngredients.Contains(recipe.baseCone)) return false;

        List<IceCreamIngredient> flavorCopy = new(recipe.flavors);
        List<IceCreamIngredient> stackFlavors = addedIngredients
            .Where(i => i.type == IngredientType.Flavor).ToList();

        foreach (var flavor in stackFlavors)
        {
            if (!flavorCopy.Remove(flavor)) return false;
        }

        if (flavorCopy.Count > 0) return false;

        List<IceCreamIngredient> toppingCopy = new(recipe.toppings);
        List<IceCreamIngredient> stackToppings = addedIngredients
            .Where(i => i.type == IngredientType.Topping).ToList();

        foreach (var topping in stackToppings)
        {
            if (!toppingCopy.Remove(topping)) return false;
        }

        return toppingCopy.Count == 0;
    } 
}