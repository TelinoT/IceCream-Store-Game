using System;
using UnityEngine;

public class DraggableBase : MonoBehaviour
{
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
        
        //transform.localScale = Vector3.one * 1.2f;
    }
    
    public void MoveTo(Vector2 screenPosition)
    {
        if (placed) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        
        // --- FIX IS HERE ---
        // Instead of the floor (0), we create a plane at the EXACT height the object will be (yOffset).
        // This solves the parallax issue with angled cameras.
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, yOffset, 0));

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPos = ray.GetPoint(enter);

            // 1. Apply your X Limits (Counter/Table boundaries)
            if (targetPos.x > -2f) targetPos.x = -2f;
            if (targetPos.x < -3f) targetPos.x = -3f;

            // 2. Apply Coordinates
            // We keep your +0.5f offset because you asked to restore it.
            float finalX = targetPos.x; 
            float finalY = yOffset; // The height is already correct from the plane
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
                    PlaceToppings();
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
        Debug.Log("Adding Sprinkles: DraggableBase");
        placed = true;
        stack.AddIngredient(ingredient, this.gameObject);
        
        IceCreamStack.Instance.AddSprinkles();
        
        AudioManager.Instance.Play("SprinklesDrop");
        
        TaskManager.Instance.ReportProgress(TaskGoalType.AddSprinkles, 1);
        
        Destroy(gameObject);
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
}
