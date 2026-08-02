using UnityEngine;
using System.Collections;

public class RandomSpawner : MonoBehaviour
{
    [Header("Prefab List")]
    public GameObject[] prefabList;

    [Header("Spawn Settings")]
    public int numberToSpawn = 8;
    public Transform[] spawnPoints;

    void OnEnable()
    {
        if (spawnPoints.Length != 4)
        {
            Debug.LogWarning("You must assign exactly 4 spawn points.");
            return;
        }

        StartCoroutine(SpawnObjectsWithCycle());
    }

    IEnumerator SpawnObjectsWithCycle()
    {
        for (int i = 0; i < numberToSpawn; i++)
        {
            int prefabIndex = Random.Range(0, prefabList.Length);
            int spawnPointIndex = i % spawnPoints.Length;

            Transform spawnPoint = spawnPoints[spawnPointIndex];
            GameObject obj = Instantiate(prefabList[prefabIndex], spawnPoint.position, Quaternion.identity);

            // Parent the object to this spawner
            obj.transform.SetParent(this.transform);

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
                rb = obj.AddComponent<Rigidbody>();

            StartCoroutine(DeactivateRigidbodyAfterDelay(rb, 1f));

            yield return null;
        }
    }

    IEnumerator DeactivateRigidbodyAfterDelay(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}