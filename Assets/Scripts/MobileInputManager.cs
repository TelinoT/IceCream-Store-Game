using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    // --- NEW: Added Singleton ---
    public static MobileInputManager Instance;

    [Header("References")]
    public Camera mainCam;
    public CameraSwipeMover cameraMover;

    [Header("Settings")]
    public LayerMask dispenserLayer; 
    public float edgeScrollSpeed = 10f;
    public float edgeBoundary = 100f;

    private DraggableBase currentItem;
    private bool isDraggingItem = false;
    private Vector2 lastTouchPos;

    // --- NEW: Set up the Singleton ---
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

        if (Physics.Raycast(ray, out hit, 100f, dispenserLayer))
        {
            IngredientDispenser dispenser = hit.collider.GetComponent<IngredientDispenser>();
            if (dispenser != null)
            {
                bool needsBase = !IceCreamStack.Instance.HasBase();
                bool isBase = dispenser.ingredient.type == IngredientType.Base;
                
                if ((needsBase && isBase) || (!needsBase && !isBase))
                {
                    StartDraggingItem(dispenser);
                    return; 
                }
            }
        }

        isDraggingItem = false;
        lastTouchPos = screenPos;
    }

    void HandleTouchMove(Vector2 screenPos)
    {
        if (isDraggingItem && currentItem != null)
        {
            currentItem.MoveTo(screenPos);
            HandleEdgeScrolling(screenPos);
        }
        else
        {
            float deltaX = screenPos.x - lastTouchPos.x;
            if (cameraMover != null)
                cameraMover.ManualMove(deltaX);

            lastTouchPos = screenPos;
        }
    }

    void HandleTouchEnd()
    {
        if (isDraggingItem && currentItem != null)
        {
            currentItem.TryPlace();
        }
        isDraggingItem = false;
        currentItem = null;
    }

    void StartDraggingItem(IngredientDispenser dispenser)
    {
        currentItem = dispenser.SpawnIngredient();
        if (currentItem != null)
        {
            isDraggingItem = true;
            currentItem.MoveTo(Input.mousePosition);
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

    // --- NEW: Method to forcefully stop dragging ---
    public void CancelDrag()
    {
        if (isDraggingItem && currentItem != null)
        {
            // Destroy the ingredient floating on their finger
            Destroy(currentItem.gameObject); 
            currentItem = null;
            isDraggingItem = false;
        }
    }
}