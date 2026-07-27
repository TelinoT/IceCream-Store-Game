using UnityEngine;

public class Buttons : MonoBehaviour
{
    public static Buttons Instance;

    public CustomerOrder currentCustomer;
    public IceCreamStack stack;
    
    [Header("World Space UI")]
    public GameObject serveUICanvas; 
    
    [Header("Optional Target Transform Settings")]
    public Vector3 targetPosition;
    public Vector3 targetRotationEuler = new Vector3(0f, 180f, 0f);

    private Vector3 originalPosition;
    private Vector3 originalRotationEuler;
    private bool isRotated = false;

    void Awake()
    {
        Instance = this;
    }
    
    public void UpdateServeUI()
    {
        if (serveUICanvas != null)
        {
            // Only show the button if there is a customer AND the plate is not empty
            bool hasCustomer = currentCustomer != null;
            bool hasIceCream = IceCreamStack.Instance != null && IceCreamStack.Instance.addedIngredients.Count > 0;
            
            serveUICanvas.SetActive(hasCustomer && hasIceCream);
        }
    }

    void Start()
    {
        originalPosition = Camera.main.transform.position;
        originalRotationEuler = Camera.main.transform.eulerAngles;
        
        // Ensure the button is hidden when the game starts
        if (serveUICanvas != null) serveUICanvas.SetActive(false);
    }

    public void RotateCamera180()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.CancelDrag();
        }

        if (!isRotated)
        {
            originalPosition = Camera.main.transform.position;
            originalRotationEuler = Camera.main.transform.eulerAngles;
            
            Camera.main.transform.position = targetPosition;
            Camera.main.transform.eulerAngles = targetRotationEuler;

            isRotated = true;
            CameraSwipeMover.Instance.currentInput = -1;
            
            // Move stack back to the prep station
            if (IceCreamStack.Instance != null) IceCreamStack.Instance.MoveToCounter(false);
            
            PersistentUIController.Instance.HideUI();
        }
        else
        {
            Camera.main.transform.position = originalPosition;
            Camera.main.transform.eulerAngles = originalRotationEuler;
            isRotated = false;
            
            CameraSwipeMover.Instance.currentInput = 1;
            
            // Move stack to the front customer counter
            if (IceCreamStack.Instance != null) IceCreamStack.Instance.MoveToCounter(true);
            
            PersistentUIController.Instance.ShowUI();
        }
    }
    
    public void TrashIceCream()
    {
        if (IceCreamStack.Instance.addedIngredients.Count > 0)
        {
            IceCreamStack.Instance.ResetStack();
            float restoreAmount = UpgradeManager.Instance.GetCurrentStatValueByID("trash_patience");
            if (restoreAmount > 0 && currentCustomer != null) currentCustomer.RestorePatience(restoreAmount);
        }
    }

    public void PlaySound()
    {
        AudioManager.Instance.Play("ButtonPop");
    }
    
    public void PlayTrashSound()
    {
        AudioManager.Instance.Play("DeleteButton");
    }

    public void Serve()
    {
        if (currentCustomer != null && stack != null)
        {
            currentCustomer.ReceiveOrder(stack);
        }
        // No longer forcing RotateCamera180() here!
    }
}