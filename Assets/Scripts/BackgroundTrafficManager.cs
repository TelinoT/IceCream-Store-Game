using UnityEngine;
using System.Collections;

public class BackgroundTrafficManager : MonoBehaviour
{
    [Header("Car Prefabs")]
    [Tooltip("Drop your different car models or colors in here!")]
    public GameObject[] carPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Place an empty GameObject off-screen to the left, facing RIGHT.")]
    public Transform leftSpawnPoint;
    
    [Tooltip("Place an empty GameObject off-screen to the right, facing LEFT.")]
    public Transform rightSpawnPoint;

    [Header("Traffic Settings")]
    public float minSpawnDelay = 4f;
    public float maxSpawnDelay = 12f;
    
    public float minSpeed = 3f;
    public float maxSpeed = 7f;

    void Start()
    {
        if (carPrefabs.Length > 0)
        {
            StartCoroutine(SpawnTrafficRoutine());
        }
        else
        {
            Debug.LogWarning("BackgroundTrafficManager has no car prefabs assigned!");
        }
    }

    private IEnumerator SpawnTrafficRoutine()
    {
        while (true)
        {
            // 1. Wait for a random amount of time
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);

            // 2. Pick a random car
            GameObject selectedCarPrefab = carPrefabs[Random.Range(0, carPrefabs.Length)];

            // 3. Decide direction (50/50 chance)
            bool comingFromLeft = Random.value > 0.5f;
            Transform spawnPoint = comingFromLeft ? leftSpawnPoint : rightSpawnPoint;

            // 4. Spawn the car at the chosen point
            GameObject newCar = Instantiate(selectedCarPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // 5. Add the movement script and set a random speed
            CarMover mover = newCar.AddComponent<CarMover>();
            mover.speed = Random.Range(minSpeed, maxSpeed);
        }
    }
}