using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "IceCream/Recipe")]
public class IceCreamRecipe : ScriptableObject
{
    public string recipeName;
    public IceCreamIngredient baseCone;
    public List<IceCreamIngredient> flavors;
    public List<IceCreamIngredient> toppings;
    public int price;
    
    [Header("Customer Dialogue")]
    public string[] orderLines;       // E.g. "I’d like strawberry and chocolate please!"
    public string[] correctResponseLines; // E.g. "Yum! Thanks!"
    public string[] wrongResponseLines;
}