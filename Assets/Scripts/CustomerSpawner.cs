using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    public List<IceCreamRecipe> possibleRecipes;
    public GameObject customerPrefab;
    public Transform spawnPoint;

    public void SpawnCustomer()
    {
        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        customer.transform.Rotate(0f, 90f, 0f);
        CustomerOrder order = customer.GetComponent<CustomerOrder>();
        order.desiredRecipe = possibleRecipes[Random.Range(0, possibleRecipes.Count)];

        Buttons.Instance.currentCustomer = order;
        
        AudioManager.Instance.Play("Talking");

    }
}