using UnityEngine;

public enum IngredientType { Base, Flavor, Topping }

[CreateAssetMenu(fileName = "NewIngredient", menuName = "IceCream/Ingredient")]
public class IceCreamIngredient : ScriptableObject
{
    public string ingredientName;
    public IngredientType type;
    public GameObject prefab; // Visual scoop, cone, topping
    public Sprite icon;       // For UI
    
    [Tooltip("How much this ingredient should move up from the previous one.")]
    public float stackHeight = 0.5f; // Default height between stacked items
}
