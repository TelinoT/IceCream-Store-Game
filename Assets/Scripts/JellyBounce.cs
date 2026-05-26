using UnityEngine;
using System.Collections;

public class JellyBounce : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 0.2f; // Fast recovery (snappy)
    
    // SPLAT SETTINGS:
    // Y is much smaller (0.7 = flattened down)
    // X/Z are wider (1.15 = spreading out)
    public Vector3 splatMultipliers = new Vector3(1.05f, 0.9f, 1.05f); 

    public void PlayBounce()
    {
        StopAllCoroutines();
        StartCoroutine(SplatRoutine());
    }

    IEnumerator SplatRoutine()
    {
        // 1. Capture the "Normal" size (e.g. 0.5, 0.5, 0.5)
        Vector3 baseScale = transform.localScale; 
        
        // 2. Calculate the "Splat" size
        Vector3 splatScale = new Vector3(
            baseScale.x * splatMultipliers.x,
            baseScale.y * splatMultipliers.y,
            baseScale.z * splatMultipliers.z
        );

        // 3. INSTANT IMPACT (Fixes the delay)
        // We set it to the squashed shape immediately on frame 1
        transform.localScale = splatScale;
        
        yield return null; // Wait one frame to let the player see the impact

        // 4. RECOVERY (Ooze back to normal)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // "Ease Out" math: Starts fast, slows down at the end
            // This makes the recovery look heavy/sticky
            float smoothT = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); 
            
            transform.localScale = Vector3.Lerp(splatScale, baseScale, smoothT);
            
            yield return null;
        }
        
        // 5. Done
        transform.localScale = baseScale; 
    }
}