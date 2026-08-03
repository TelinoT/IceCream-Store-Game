using UnityEngine;
using System.Collections;

public class SyrupManager : MonoBehaviour
{
    public static SyrupManager Instance;

    [Header("Camera & UI")]
    public Camera mainCam;
    public Camera topDownCamTarget; 
    public GameObject syrupUIPanel; // Assign a UI panel containing your "Done" button
    public float transitionDuration = 0.4f;

    [Header("Syrup Settings")]
    public GameObject syrupTrailPrefab; // A prefab with just a TrailRenderer component on it
    public LayerMask iceCreamLayer;     // The layer your ice cream colliders are on
    public float minDistanceBetweenPoints = 0.05f;

    private bool isSyrupModeActive = false;
    private LineRenderer currentLine;
    private Vector3 lastDrawPoint;

    // Camera Memory
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private float originalCamFOV;

    // State Memory
    private GameObject heldSyrupBottle;
    private IceCreamIngredient currentIngredient;
    
    public float surfaceOffset = 0.025f;

    void Awake()
    {
        Instance = this;
        if (syrupUIPanel != null) syrupUIPanel.SetActive(false);
    }

    public void EnterSyrupMode(GameObject bottleObj, IceCreamIngredient ingredient)
    {
        isSyrupModeActive = true;
        heldSyrupBottle = bottleObj;
        currentIngredient = ingredient;

        // --- FIX: Hide the dragged bottle instantly! ---
        if (heldSyrupBottle != null) heldSyrupBottle.SetActive(false);

        // 1. Lock regular inputs & dragging
        MobileInputManager.Instance.enabled = false;
        CameraSwipeMover.Instance.currentInput = -1;

        // 2. Pause Customer Patience and hide the slider
        if (Buttons.Instance != null && Buttons.Instance.currentCustomer != null)
        {
            Buttons.Instance.currentCustomer.isTimerPaused = true;
        }
        if (PatienceUIManager.Instance != null)
        {
            PatienceUIManager.Instance.HideSlider();
        }

        // 3. Move Camera
        StartCoroutine(TransitionToTopDown());
    }

    private IEnumerator TransitionToTopDown()
    {
        originalCamPos = mainCam.transform.position;
        originalCamRot = mainCam.transform.rotation;
        originalCamFOV = mainCam.fieldOfView;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            mainCam.transform.position = Vector3.Lerp(originalCamPos, topDownCamTarget.transform.position, easeT);
            mainCam.transform.rotation = Quaternion.Lerp(originalCamRot, topDownCamTarget.transform.rotation, easeT);
            mainCam.fieldOfView = Mathf.Lerp(originalCamFOV, topDownCamTarget.fieldOfView, easeT);
            yield return null;
        }

        // Lock exactly to the target
        mainCam.transform.position = topDownCamTarget.transform.position;
        mainCam.transform.rotation = topDownCamTarget.transform.rotation;
        mainCam.fieldOfView = topDownCamTarget.fieldOfView;

        syrupUIPanel.SetActive(true);

        // Hide the held bottle so it doesn't block our top-down view
        if (heldSyrupBottle != null) heldSyrupBottle.SetActive(false);
    }

    void Update()
    {
        if (!isSyrupModeActive || !syrupUIPanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0)) StartDrawing();
        else if (Input.GetMouseButton(0)) ContinueDrawing();
        else if (Input.GetMouseButtonUp(0)) StopDrawing();
    }

    private void StartDrawing()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, iceCreamLayer))
        {
            GameObject trailObj = Instantiate(syrupTrailPrefab, IceCreamStack.Instance.visualParent);
            trailObj.transform.localPosition = Vector3.zero;
            trailObj.transform.localRotation = Quaternion.identity;
            
            currentLine = trailObj.GetComponent<LineRenderer>();
            currentLine.positionCount = 1;
            
            Vector3 drawPos = hit.point + (hit.normal * surfaceOffset);
            
            Vector3 localHit = currentLine.transform.InverseTransformPoint(drawPos);
            currentLine.SetPosition(0, localHit);
            
            // Save the original hit point for distance calculations
            lastDrawPoint = hit.point; 
        }
    }

    private void ContinueDrawing()
    {
        if (currentLine == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, iceCreamLayer))
        {
            if (Vector3.Distance(hit.point, lastDrawPoint) > minDistanceBetweenPoints)
            {
                currentLine.positionCount++;
                
                // --- FIX: Push the point outward along the surface normal ---
                Vector3 drawPos = hit.point + (hit.normal * surfaceOffset);
                
                Vector3 localHit = currentLine.transform.InverseTransformPoint(drawPos);
                currentLine.SetPosition(currentLine.positionCount - 1, localHit);
                
                lastDrawPoint = hit.point;
            }
        }
    }

    private void StopDrawing()
    {
        currentLine = null;
    }

    // --- CALL THIS FROM YOUR UI "DONE" BUTTON ---
    public void FinishSyrupPhase()
    {
        syrupUIPanel.SetActive(false);
        isSyrupModeActive = false;

        // Log the syrup in the recipe checker
        IceCreamStack.Instance.AddIngredient(currentIngredient, heldSyrupBottle);
        
        // (Optional: We leave the hidden bottle object alive in the list so the stack knows it's there, 
        // but it stays invisible. Only the trails are seen.)

        StartCoroutine(TransitionBack());
    }

    private IEnumerator TransitionBack()
    {
        float elapsed = 0f;
        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;
        float startFOV = mainCam.fieldOfView;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            mainCam.transform.position = Vector3.Lerp(startPos, originalCamPos, easeT);
            mainCam.transform.rotation = Quaternion.Lerp(startRot, originalCamRot, easeT);
            mainCam.fieldOfView = Mathf.Lerp(startFOV, originalCamFOV, easeT);
            yield return null;
        }

        // Restore Camera
        mainCam.transform.position = originalCamPos;
        mainCam.transform.rotation = originalCamRot;
        mainCam.fieldOfView = originalCamFOV;

        // Restore Inputs
        MobileInputManager.Instance.enabled = true;
        CameraSwipeMover.Instance.currentInput = 1;

        // Unpause Patience
        if (Buttons.Instance != null && Buttons.Instance.currentCustomer != null)
        {
            Buttons.Instance.currentCustomer.isTimerPaused = false;
            
            // Slide the patience UI back in!
            if (PatienceUIManager.Instance != null)
            {
                PatienceUIManager.Instance.ShowSlider(Buttons.Instance.currentCustomer.maxPatience);
            }
        }
    }
}