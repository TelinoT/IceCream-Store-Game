using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class IceCreamStack : MonoBehaviour
{
    public List<IceCreamIngredient> addedIngredients = new();
    public List<GameObject> addedObjects = new();
    public Transform visualParent;

    public static bool hasCone;
    
    private float currentHeight = 0f;
    
    public static IceCreamStack Instance;

    
    void Awake()
    {
        Instance = this;
        hasCone = false;
    }

    public bool HasBase() => addedIngredients.Any(i => i.type == IngredientType.Base);

    // Use this when adding from base dragging (you already have the visual object)
    public void AddIngredient(IceCreamIngredient ingredient, GameObject visualObj)
    {
        if (ingredient.type == IngredientType.Base && HasBase()) return;
        if (ingredient.type != IngredientType.Base && !HasBase()) return;

        addedIngredients.Add(ingredient);
        addedObjects.Add(visualObj);

        /*if (visualObj != null)
        {
            visualObj.transform.SetParent(visualParent);
            visualObj.transform.localPosition = Vector3.zero;
            currentHeight = 0f;
        }*/
    }

    // Use this when adding from flavor/topping dispensers
    public void AddIngredient(IceCreamIngredient ingredient)
    {
        if (ingredient.type != IngredientType.Base && !HasBase()) return;

        addedIngredients.Add(ingredient);

        if (ingredient.prefab != null && visualParent != null)
        {
            currentHeight += ingredient.stackHeight;

            GameObject obj = Instantiate(ingredient.prefab, visualParent);
            obj.transform.localPosition = new Vector3(0, currentHeight, 0);

            //obj.transform.localRotation = Quaternion.Euler(0, Random.Range(-10f, 10f), 0);
            
            obj.transform.localRotation = Quaternion.Euler(
                Random.Range(-8f, 8f),   // Slight tilt forward/back
                Random.Range(0f, 360f),  // Random facing direction
                Random.Range(-8f, 8f)    // Slight tilt left/right
            );
        }
    }

    public void AddSprinkles()
    {
        //Debug.Log("Adding sprinkles: IceCreamStack");
        /*foreach (var element in addedObjects)
        {
            if (element.transform.childCount > 0)
            {
                Transform firstChild = element.transform.GetChild(0);
                firstChild.gameObject.SetActive(true);
            }
        }*/
        
        for (int i = 0; i < addedObjects.Count; i++)
        {
            // Skip if this is the Cone or Cup (Base)
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
