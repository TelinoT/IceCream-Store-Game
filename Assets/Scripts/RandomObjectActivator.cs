using UnityEngine;
using System.Collections.Generic;

public class RandomObjectActivator : MonoBehaviour
{
    [Tooltip("List of objects to choose from")]
    public List<GameObject> objects = new List<GameObject>();

    void Start()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("No objects assigned to RandomObjectActivator.");
            return;
        }

        // Deactivate all objects first
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Pick a random one and activate it
        int randomIndex = Random.Range(0, objects.Count);
        if (objects[randomIndex] != null)
            objects[randomIndex].SetActive(true);
    }
}