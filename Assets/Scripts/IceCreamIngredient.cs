using UnityEngine;

public enum IngredientType { Base, Flavor, Topping }

[CreateAssetMenu(fileName = "NewIngredient", menuName = "IceCream/Ingredient")]
public class IceCreamIngredient : ScriptableObject
{
    public string ingredientName;
    public IngredientType type;
    public GameObject prefab; 
    public Sprite icon;       
    
    [Tooltip("How much this ingredient should move up from the previous one.")]
    public float stackHeight = 0.5f; 

    // --- NEW: Dynamic Pricing ---
    [Tooltip("How much this specific ingredient adds to the total order price.")]
    public int price; 
}