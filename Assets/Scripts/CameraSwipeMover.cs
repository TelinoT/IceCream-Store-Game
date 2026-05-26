using UnityEngine;

public class CameraSwipeMover : MonoBehaviour
{
    public static CameraSwipeMover Instance;

    [Header("Swipe Settings")]
    public float swipeSpeed = 0.1f;
    public float maxOffsetRight = 5f;
    public float maxOffsetLeft = 5f;
    private float sensitivityFactor = 1f;

    [Header("Perspective Fix")]
    // NEW: When controls are flipped, we multiply speed by this (0.85 = 15% slower)
    // This counteracts the visual effect of close objects moving faster.
    public float flippedDragMultiplier = 0.85f; 

    private Vector3 initialCameraPosition;
    public float currentInput; 
    
    [Header("Rubber Band Settings")]
    public float overdragResistance = 0.25f; 
    public float snapBackSpeed = 10f; 
    
    private bool isBeingDragged = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        initialCameraPosition = transform.position;
        currentInput = 1;
    }

    void Update()
    {
        if (!isBeingDragged)
        {
            float currentZ = transform.position.z;
            float minZ = initialCameraPosition.z - maxOffsetRight;
            float maxZ = initialCameraPosition.z + maxOffsetLeft;

            float targetZ = currentZ;
            bool needsSnap = false;

            if (currentZ < minZ) { targetZ = minZ; needsSnap = true; }
            else if (currentZ > maxZ) { targetZ = maxZ; needsSnap = true; }

            if (needsSnap)
            {
                float newZ = Mathf.Lerp(currentZ, targetZ, snapBackSpeed * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
            }
        }
        isBeingDragged = false;
    }

    public void ManualMove(float deltaX)
    {
        isBeingDragged = true;

        float finalDelta = ApplyDirectionFlip(deltaX);
        float moveAmount = finalDelta * swipeSpeed * Time.deltaTime; 
        
        moveAmount *= sensitivityFactor;

        // --- NEW: PERSPECTIVE FIX ---
        // If controls are flipped (currentInput is -1), reduce speed slightly
        // because moving "backwards/down" visually looks faster.
        if (currentInput < 0)
        {
            moveAmount *= flippedDragMultiplier;
        }
        // ----------------------------

        float currentZ = transform.position.z;
        float minZ = initialCameraPosition.z - maxOffsetRight;
        float maxZ = initialCameraPosition.z + maxOffsetLeft;

        if (currentZ < minZ && moveAmount < 0) moveAmount *= overdragResistance;
        else if (currentZ > maxZ && moveAmount > 0) moveAmount *= overdragResistance;

        Vector3 newPosition = transform.position;
        newPosition.z += moveAmount;
        transform.position = newPosition;
    }

    public float ApplyDirectionFlip(float input)
    {
        return currentInput * input;
    }

    public void SetSensitivityFactor(float value)
    {
        sensitivityFactor = value;
    }
}