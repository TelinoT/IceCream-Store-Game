using UnityEngine;
using System.Collections.Generic;

public class SprinkleGroup : MonoBehaviour
{
    private List<GameObject> hiddenSprinkles = new List<GameObject>();

    void Awake()
    {
        // Gather all child sprinkles and hide them immediately
        foreach (Transform child in transform)
        {
            hiddenSprinkles.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }
    }

    public void RevealRandom(float percentage)
    {
        // Calculate how many total sprinkles should be visible at this percentage
        int targetActive = Mathf.RoundToInt(transform.childCount * percentage);
        int currentlyActive = transform.childCount - hiddenSprinkles.Count;
        int toActivate = targetActive - currentlyActive;

        // Pick random sprinkles from the hidden list and turn them on
        for (int i = 0; i < toActivate; i++)
        {
            if (hiddenSprinkles.Count == 0) break;

            int randomIndex = Random.Range(0, hiddenSprinkles.Count);
            GameObject sprinkle = hiddenSprinkles[randomIndex];
            
            sprinkle.SetActive(true);
            hiddenSprinkles.RemoveAt(randomIndex);
        }
    }
}