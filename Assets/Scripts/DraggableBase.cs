using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class DraggableBase : MonoBehaviour
{
    [Header("Carving Settings")]
    private float carveHeight = 1.2f; // Tweak this so it sits perfectly above your tubs
    private float activeYOffset;
    
    public bool isShakingMode = false;
    private int shakeCount = 0;
    private int requiredShakes = 3;
    
    private Coroutine shakeCoroutine;
    private bool isFinishing = false;
    
    private IceCreamIngredient ingredient;
    private bool placed = false;

    public bool isCone = false;

    private bool onTopBase = false;

    private IceCreamStack stack;
    private Transform plateTransform;

    public float yOffset = 0.01f; // small height above surface
    
    public float yOffsetCup = 0.01f; // small height above surface
    public float yOffsetCone = 0.01f; // small height above surface
    
    private float nudgeX = 0f;
    private float nudgeZ = 0f;

    // Heights from which to raycast downward for Y pos adjustment
    public float raycastHeightOffset = 5f;
    public float maxRaycastDistance = 10f;
    
    private Vector3 targetPos = new Vector3();
    
    private Vector3 originalScale;
    
    public void Initialize(IceCreamIngredient ingredient)
    {
        this.ingredient = ingredient;
        stack = GameObject.Find("IceCreamStack").GetComponent<IceCreamStack>();
        plateTransform = GameObject.FindWithTag("Plate")?.transform;

        CameraSwipeMover.Instance.currentInput = 1;
        
        originalScale = transform.localScale;
        
        onTopBase = false;

        if (ingredient.type == IngredientType.Flavor)
        {
            if (IceCreamStack.hasCone)
            {
                yOffset = yOffsetCone;
            }
            else
            {
                yOffset = yOffsetCup;
            }
            
            int flavorCount = stack.GetFlavorCount();
            if (flavorCount == 1)
            {
                // 2nd Scoop: Pull the drag plane down so it visually overlaps
                yOffset += (-ingredient.stackHeight + 0.05f);
                
                // Roll a random horizontal nudge to apply during the drag
                nudgeX = UnityEngine.Random.Range(-0.12f, 0.12f);
                nudgeZ = UnityEngine.Random.Range(-0.12f, 0.12f);
            }
            else if (flavorCount >= 2)
            {
                // 3rd Scoop: Push the drag plane up so it sits on top
                yOffset += (ingredient.stackHeight * 0.5f);
            }
        }
        
        activeYOffset = yOffset;
        
        //transform.localScale = Vector3.one * 1.2f;
    }
    
    public void SetCarveProgress(float percentage)
    {
        // Smoothly scale from 0 to its true original scale based on drag distance
        transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, percentage);
        
        // --- NEW: Height Logic ---
        if (percentage >= 1f)
        {
            // Carving is done, drop to the correct stack height for the plate
            activeYOffset = yOffset;
        }
        else
        {
            // Still carving, stay elevated safely above the tubs
            activeYOffset = carveHeight;
        }
    }

    public void MoveTo(Vector2 screenPosition)
    {
        if (placed) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        
        // --- CHANGED: We now use the dynamic activeYOffset instead of the static yOffset ---
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, activeYOffset, 0));

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPos = ray.GetPoint(enter);

            // 1. Apply your X Limits (Counter/Table boundaries)
            if (targetPos.x > -2f) targetPos.x = -2f;
            if (targetPos.x < -3f) targetPos.x = -3f;

            // 2. Apply Coordinates
            float finalX = targetPos.x; 
            float finalY = activeYOffset; // --- CHANGED ---
            float finalZ = targetPos.z;

            // 3. Update Position
            transform.position = new Vector3(finalX, finalY, finalZ);
            transform.rotation = Quaternion.identity;
        }
    }
    
    /*

    // Call this every frame while dragging
    public void MoveTo(Vector3 screenPosition)
    {
        if (placed) return;

        // 1) Raycast from camera through mouse position to get a stable X,Z point on a horizontal plane or floor
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // horizontal plane at Y=0 (adjust if your floor is different)

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPos = ray.GetPoint(enter);

            // 2) Now from target X,Z, raycast down from high above to find actual surface height below
            float rayStartHeight = 10f;  // start high enough above all surfaces
            Vector3 rayStartPos = new Vector3(targetPos.x, rayStartHeight, targetPos.z);
            Ray downRay = new Ray(rayStartPos, Vector3.down);

            // 3) Clamp X movement to keep it in range
            if (targetPos.x > -2.5f)
            {
                targetPos.x = -2.5f;
            }

            if (targetPos.x < -3.5f)
            {
                targetPos.x = -3.5f;
            }

            // 4) Scale Z movement to reduce sensitivity
            float movementScaleZ = 0.1f; // Lower = less left/right movement
            float scaledZ = transform.position.z + (targetPos.z - transform.position.z) * movementScaleZ;

            // 5) Compute final position (with +0.5f offset on X as in original)
            float finalX = targetPos.x + 0.5f;
            float finalY = yOffset;
            float finalZ = scaledZ;

            if (Physics.Raycast(downRay, out RaycastHit hit, 20f))
            {
                transform.position = new Vector3(finalX, finalY, finalZ);
            }
            else
            {
                transform.position = new Vector3(finalX, finalY, finalZ);
            }

            // 6) Maintain fixed rotation
            transform.rotation = Quaternion.identity;

            // Optional: debug Z
            //Debug.Log(finalZ);
        }
    }
    
    void LateUpdate()
    {
        if (placed) return;

        // Raycast downward from above the current X,Z position to find surface Y
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + raycastHeightOffset, transform.position.z);

        // Make sure rotation stays fixed (optional: set to zero or initial rotation)
        transform.rotation = Quaternion.identity; // or save initial rotation if needed
    }

*/
    public void TryPlace()
    {
        CameraSwipeMover.Instance.currentInput = -1;
        
        if (placed) return;
        
        //transform.localScale = Vector3.one;

        if (ingredient.type == IngredientType.Base)
        {
            if (plateTransform != null)
            {
                float distance = Vector3.Distance(transform.position, plateTransform.position);
                if (distance < 0.5f)
                {
                    PlaceOnPlate();
                    return;
                }
            }
        }

        if (ingredient.type == IngredientType.Flavor)
        {
            if (onTopBase)
            {
                PlaceOnPlate();
                return;
            }
        }

        if (ingredient.type == IngredientType.Topping)
        {
            if (plateTransform != null)
            {
                float distance = Vector3.Distance(transform.position, plateTransform.position);
                if (distance < 0.5f)
                {
                    // --- CHANGED: Route the logic based on the specific topping's needs! ---
                    switch (ingredient.interactionType)
                    {
                        case ToppingInteraction.Shaker:
                            EnterShakerMode();
                            break;
    
                        case ToppingInteraction.InstantDrop:
                            PlaceToppings(); 
                            break;

                        case ToppingInteraction.TracePath:
                            SyrupManager.Instance.EnterSyrupMode(this.gameObject, ingredient);
                            break;
                    }
                
                    return;
                }
            }
        }

        // Destroy if not placed on plate
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Base")
        {
            onTopBase = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Base")
        {
            onTopBase = false;
        }
    }

    private void PlaceToppings()
    {
        placed = true;

        if (IceCreamStack.Instance != null)
        {
            IceCreamStack.Instance.RevealSprinkles(1f);
        }

        transform.SetParent(stack.visualParent);
        transform.localScale = originalScale;

        stack.AddIngredient(ingredient, this.gameObject);
        
        //AudioManager.Instance.Play("BaseDrop");
    }

    private void PlaceOnPlate()
    {
        if (isCone)
        {
            IceCreamStack.hasCone = true;
        }

        if (ingredient.type == IngredientType.Base)
        {
            AudioManager.Instance.Play("BaseDrop");
        }
        
        placed = true;

        // Snap exactly to plate position
        //transform.position = plateTransform.position + Vector3.up * yOffset;

        transform.SetParent(stack.visualParent);
        
        transform.localScale = originalScale;

        stack.AddIngredient(ingredient, this.gameObject);

        if (ingredient.type == IngredientType.Flavor)
        {
            AudioManager.Instance.Play("ScoopPlace");
        }
        
        JellyBounce jelly = GetComponent<JellyBounce>();
        if (jelly != null) jelly.PlayBounce();
    }
    
    private void EnterShakerMode()
    {
        isShakingMode = true;
        CameraSwipeMover.Instance.currentInput = 1; // Release the camera lock
        
        // 1. Calculate how tall the current ice cream stack is 
        // (Assuming each flavor scoop adds roughly 0.45f in height)
        float currentIceCreamHeight = 0f;
        if (stack != null)
        {
            // We multiply the number of scoops by an estimated scoop height
            currentIceCreamHeight = stack.GetFlavorCount() * 0.025f;
        }
        
        Vector3 screenRight = Camera.main.transform.right;
        screenRight.y = 0f; // Keep it perfectly flat so it doesn't mess with our height
        screenRight.Normalize();

        // 2. Float just slightly (0.4f) above the top of the ice cream stack
        transform.position = plateTransform.position + Vector3.up * (currentIceCreamHeight + 0.5f) + (screenRight * 0.05f);
        
        transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        
        // Tell the stack it has toppings, but don't delete the jar yet
        stack.AddIngredient(ingredient, this.gameObject); 
    }

    public void PerformShake()
    {
        // Don't allow more taps if it's currently doing its final exit animation
        if (isFinishing) return;

        shakeCount++;
        float progress = (float)shakeCount / requiredShakes;

        IceCreamStack.Instance.RevealSprinkles(progress);
        AudioManager.Instance.Play("SprinklesTake"); 

        // --- CHANGED: Trigger the smooth shake animation ---
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(SmoothShakeRoutine());

        if (shakeCount >= requiredShakes)
        {
            isFinishing = true;
            // Delay the destruction by 0.2 seconds so the final shake animation actually finishes!
            Invoke("FinishTopping", 0.2f); 
        }
    }
    
    private IEnumerator SmoothShakeRoutine()
    {
        float duration = 0.15f; 
        float elapsed = 0f;
        
        // The resting tilt
        Quaternion baseRot = Quaternion.Euler(-55f, 0f, 0f);
        // The downward flick of the wrist
        Quaternion shakeRot = Quaternion.Euler(-95f, 0f, 0f); 

        // 1. Flick downwards
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            transform.localRotation = Quaternion.Lerp(baseRot, shakeRot, t);
            yield return null;
        }
        
        // 2. Snap smoothly back to resting position
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            transform.localRotation = Quaternion.Lerp(shakeRot, baseRot, t);
            yield return null;
        }

        // Lock it perfectly back to 35 degrees
        transform.localRotation = baseRot;
    }

    private void FinishTopping()
    {
        placed = true;
        AudioManager.Instance.Play("SprinklesDrop");
        TaskManager.Instance.ReportProgress(TaskGoalType.AddSprinkles, 1);
        Destroy(gameObject);
    }
}
