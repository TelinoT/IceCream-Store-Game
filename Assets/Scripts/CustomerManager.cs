using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    public CustomerSpawner spawner;
    public float timeBetweenCustomers = 1.5f; // delay after each customer leaves

    private void Start()
    {
        //SpawnFirstCustomer();
    }

    public void SpawnFirstCustomer()
    {
        spawner.SpawnCustomer();
    }

    public void SpawnNextCustomer()
    {
        Invoke(nameof(SpawnFirstCustomer), timeBetweenCustomers);
    }
}