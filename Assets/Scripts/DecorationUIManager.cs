using System.Collections;
using UnityEngine;

public class DecorationUIManager : MonoBehaviour
{
    [Header("Camera References")]
    public Camera gameplayCamera;
    public Camera shopCamera; 
    
    [Header("UI References")]
    public GameObject shopUI;
    public DecorationExtraUIManager decorationExtraUIManager;
    public RectTransform shopUIPanel;

    [Header("Animation Settings")]
    public float transitionDuration = 0.4f;
    
    [Tooltip("Where the UI hides when closed (e.g., Bottom of screen)")]
    public Vector2 offScreenBottom = new Vector2(0f, -1500f); 

    private Vector2 originalUIPosition;
    
    // Camera state variables
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private float originalCamFOV; // Added to store the FOV
    
    private Coroutine currentAnim;
    private GameObject customer;
    private bool isAnimating = false;

    private void Start()
    {
        if (decorationExtraUIManager != null)
            decorationExtraUIManager.allDecorations = FindObjectsOfType<DecorationObject>(true);

        if (shopUIPanel != null)
            originalUIPosition = shopUIPanel.anchoredPosition;

        if (shopCamera != null)
            shopCamera.gameObject.SetActive(false);

        if (shopUI != null) shopUI.SetActive(false);
    }

    public void EnterShopMode()
    {
        if (isAnimating) return; 

        Time.timeScale = 0.01f; 
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.HideUI();
        
        customer = GameObject.Find("Capsule(Clone)");
        if (customer != null) customer.SetActive(false);
        
        shopUI.SetActive(true);
        decorationExtraUIManager.Setup();

        if (CameraSwipeMover.Instance != null) 
            CameraSwipeMover.Instance.enabled = false;

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateShopOpen());
    }

    public void ExitShopMode()
    {
        if (isAnimating) return;
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.ShowUI();

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateShopClose());
    }

    private IEnumerator AnimateShopOpen()
    {
        isAnimating = true;

        // Save Gameplay Camera's starting state
        originalCamPos = gameplayCamera.transform.position;
        originalCamRot = gameplayCamera.transform.rotation;
        originalCamFOV = gameplayCamera.fieldOfView; // Save FOV

        // Snap UI to the BOTTOM of the screen before animating
        shopUIPanel.anchoredPosition = offScreenBottom;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            // Glide Camera Position, Rotation, AND FOV
            gameplayCamera.transform.position = Vector3.Lerp(originalCamPos, shopCamera.transform.position, easeT);
            gameplayCamera.transform.rotation = Quaternion.Lerp(originalCamRot, shopCamera.transform.rotation, easeT);
            gameplayCamera.fieldOfView = Mathf.Lerp(originalCamFOV, shopCamera.fieldOfView, easeT);

            // Slide UI UP from the BOTTOM
            shopUIPanel.anchoredPosition = Vector2.Lerp(offScreenBottom, originalUIPosition, easeT);

            yield return null;
        }

        // Snap to exact final values to prevent floating point errors
        gameplayCamera.transform.position = shopCamera.transform.position;
        gameplayCamera.transform.rotation = shopCamera.transform.rotation;
        gameplayCamera.fieldOfView = shopCamera.fieldOfView;
        shopUIPanel.anchoredPosition = originalUIPosition;

        isAnimating = false;
    }

    private IEnumerator AnimateShopClose()
    {
        isAnimating = true;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            // Glide Camera back to Gameplay state (Position, Rotation, and FOV)
            gameplayCamera.transform.position = Vector3.Lerp(shopCamera.transform.position, originalCamPos, easeT);
            gameplayCamera.transform.rotation = Quaternion.Lerp(shopCamera.transform.rotation, originalCamRot, easeT);
            gameplayCamera.fieldOfView = Mathf.Lerp(shopCamera.fieldOfView, originalCamFOV, easeT);

            // Slide UI DOWN towards the BOTTOM
            shopUIPanel.anchoredPosition = Vector2.Lerp(originalUIPosition, offScreenBottom, easeT);

            yield return null;
        }

        // Snap FOV back to exact original value
        gameplayCamera.fieldOfView = originalCamFOV;

        if (decorationExtraUIManager != null) decorationExtraUIManager.ExitShop();
        if (customer != null) customer.SetActive(true);
        if (shopUI != null) shopUI.SetActive(false);
        
        if (CameraSwipeMover.Instance != null) 
            CameraSwipeMover.Instance.enabled = true;

        // --- THE ONLY PART THAT CHANGED IS HERE ---
        if (DayManager.Instance != null && DayManager.Instance.isBetweenDays)
        {
            // We are between days, so open the Night Hub back up!
            DayManager.Instance.ShowNightHubPanel(); 
        }
        else
        {
            // We are just checking the shop during the day, so resume time normally
            Time.timeScale = 1f; 
        }
        // ------------------------------------------

        isAnimating = false;
    }
}