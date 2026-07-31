using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance;

    [Header("References")]
    public Camera mainCam;
    public CameraSwipeMover cameraMover;

    [Header("Settings")]
    public LayerMask dispenserLayer; 
    public float edgeScrollSpeed = 10f;
    public float edgeBoundary = 100f;

    [Header("Carving Settings (Flavors Only)")]
    public float carveThreshold = 150f; 
    
    [Tooltip("How thick the 'fat finger' raycast is in 3D space. 0.5 is a good starting point.")]
    public float colliderForgivenessRadius = 0.5f;

    private DraggableBase currentItem;
    private bool isDraggingItem = false;
    private bool isCarving = false; 
    private Vector2 lastTouchPos;
    
    private float currentCarveDistance = 0f;  
    
    // --- RESTORED: We remember the exact 3D collider we are carving ---
    private Collider activeDispenserCollider; 

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (cameraMover == null) cameraMover = CameraSwipeMover.Instance;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleTouchStart(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            HandleTouchMove(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandleTouchEnd();
        }
    }

    void HandleTouchStart(Vector2 screenPos)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit hit;

        // The initial click can still be a precise, thin raycast
        if (Physics.Raycast(ray, out hit, 100f, dispenserLayer))
        {
            IngredientDispenser dispenser = hit.collider.GetComponent<IngredientDispenser>();
            if (dispenser != null)
            {
                bool needsBase = !IceCreamStack.Instance.HasBase();
                bool isBase = dispenser.ingredient.type == IngredientType.Base;
                
                if ((needsBase && isBase) || (!needsBase && !isBase))
                {
                    if (dispenser.ingredient.type == IngredientType.Flavor)
                    {
                        StartCarvingItem(dispenser, screenPos);
                    }
                    else
                    {
                        StartDraggingItem(dispenser, screenPos);
                    }
                    return; 
                }
            }
        }

        isDraggingItem = false;
        isCarving = false;
        lastTouchPos = screenPos;
    }

    void HandleTouchMove(Vector2 screenPos)
    {
        if (isCarving && currentItem != null)
        {
            Ray ray = mainCam.ScreenPointToRay(screenPos);
            bool isStillOverDispenser = false;
            
            // --- NEW: The "Fat Finger" SphereCastAll ---
            // This fires a thick cylinder from the camera. We use 'All' so that if it hits 
            // a neighboring tub first, it still checks if our active tub is inside the cylinder.
            RaycastHit[] hits = Physics.SphereCastAll(ray, colliderForgivenessRadius, 100f, dispenserLayer);
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == activeDispenserCollider)
                {
                    isStillOverDispenser = true;
                    break;
                }
            }

            // If the thick cylinder completely misses the target collider, cancel the carve!
            if (!isStillOverDispenser)
            {
                CancelDrag();
                return;
            }

            currentCarveDistance += Vector2.Distance(lastTouchPos, screenPos);
            float progress = Mathf.Clamp01(currentCarveDistance / carveThreshold);

            currentItem.SetCarveProgress(progress);
            currentItem.MoveTo(screenPos); 

            if (progress >= 1f)
            {
                isCarving = false;
                isDraggingItem = true;
                activeDispenserCollider = null; 
                
                AudioManager.Instance.Play("ButtonPop"); 
                
                JellyBounce jelly = currentItem.GetComponent<JellyBounce>();
                if (jelly != null) jelly.PlayBounce();
            }
        }
        else if (isDraggingItem && currentItem != null)
        {
            currentItem.MoveTo(screenPos);
            HandleEdgeScrolling(screenPos);
        }
        else
        {
            float deltaX = screenPos.x - lastTouchPos.x;
            if (cameraMover != null)
                cameraMover.ManualMove(deltaX);
        }
        
        lastTouchPos = screenPos;
    }

    void HandleTouchEnd()
    {
        if ((isCarving || isDraggingItem) && currentItem != null)
        {
            currentItem.TryPlace();
        }
        
        isDraggingItem = false;
        isCarving = false;
        activeDispenserCollider = null;
        currentItem = null;
    }

    void StartCarvingItem(IngredientDispenser dispenser, Vector2 screenPos)
    {
        currentItem = dispenser.SpawnIngredient();
        if (currentItem != null)
        {
            isCarving = true;
            isDraggingItem = false;
            
            currentCarveDistance = 0f; 
            
            // --- RESTORED: Save the actual 3D collider ---
            activeDispenserCollider = dispenser.GetComponent<Collider>(); 

            currentItem.SetCarveProgress(0f);
            currentItem.MoveTo(screenPos);
            
            lastTouchPos = screenPos; 
        }
    }

    void StartDraggingItem(IngredientDispenser dispenser, Vector2 screenPos)
    {
        currentItem = dispenser.SpawnIngredient();
        if (currentItem != null)
        {
            isDraggingItem = true;
            isCarving = false;
            
            currentItem.SetCarveProgress(1f); 
            currentItem.MoveTo(screenPos);
            
            lastTouchPos = screenPos;
        }
    }

    void HandleEdgeScrolling(Vector2 screenPos)
    {
        float scrollDir = 0;

        if (screenPos.x < edgeBoundary)
            scrollDir = -1f; 
        else if (screenPos.x > Screen.width - edgeBoundary)
            scrollDir = 1f; 

        if (scrollDir != 0 && cameraMover != null)
        {
            float speed = edgeScrollSpeed * Time.deltaTime * 50f;
            cameraMover.ManualMove(scrollDir * speed);
            
            if (isDraggingItem && currentItem != null)
                currentItem.MoveTo(screenPos);
        }
    }

    public void CancelDrag()
    {
        if ((isDraggingItem || isCarving) && currentItem != null)
        {
            currentItem.TryPlace(); 
            
            currentItem = null;
            isDraggingItem = false;
            isCarving = false;
            activeDispenserCollider = null;
        }
    }
}