using UnityEngine;

public class ChunkScatterer : MonoBehaviour
{
    [Header("Chunk Settings")]
    public GameObject chunkPrefab;
    public int chunkCount = 30;
    public float surfaceRadius = 0.45f;
    public bool topHalfOnly = true;

    [Header("Alignment Fixes")]
    [Tooltip("Move this up if the chunks are sinking into the bottom of the scoop.")]
    public Vector3 centerOffset = new Vector3(0f, 0.2f, 0f);

    [ContextMenu("Scatter Chunks Now!")]
    public void ScatterChunks()
    {
        if (chunkPrefab == null)
        {
            Debug.LogWarning("Please assign a Chunk Prefab first!");
            return;
        }

        // Automatically clear old ones so you can rapidly click "Scatter" to test layouts!
        ClearChunks(); 

        // Apply our offset so the math perfectly matches the visual model
        Vector3 sphereCenter = transform.position + centerOffset;

        for (int i = 0; i < chunkCount; i++)
        {
            Vector3 randomDirection = Random.onUnitSphere;

            if (topHalfOnly && randomDirection.y < 0)
            {
                randomDirection.y = -randomDirection.y;
            }

            Vector3 spawnPosition = sphereCenter + (randomDirection * surfaceRadius);

            // --- THE FIX: Make them lie flat against the curved surface ---
            // 1. Align the chunk's "Up" direction to point directly outward from the sphere
            Quaternion surfaceAlignment = Quaternion.FromToRotation(Vector3.up, randomDirection);
            
            // 2. Add a random spin on its own axis so they don't look uniform
            Quaternion randomSpin = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            
            // 3. Combine them
            Quaternion finalRotation = surfaceAlignment * randomSpin;

            GameObject newChunk = Instantiate(chunkPrefab, spawnPosition, finalRotation, transform);

            float randomScale = Random.Range(0.02f, 0.03f);
            newChunk.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
            newChunk.name = "ChocoChunk_" + i;
        }
        
        Debug.Log($"Successfully scattered {chunkCount} chunks!");
    }

    [ContextMenu("Clear Old Chunks")]
    public void ClearChunks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("ChocoChunk"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}