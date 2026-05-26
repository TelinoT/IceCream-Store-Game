using UnityEngine;

public class Buttons : MonoBehaviour
{
    public static Buttons Instance;

    public CustomerOrder currentCustomer;
    public IceCreamStack stack;
    
    [Header("Optional Target Transform Settings")]
    public Vector3 targetPosition;
    public Vector3 targetRotationEuler = new Vector3(0f, 180f, 0f);

    private Vector3 originalPosition;
    private Vector3 originalRotationEuler;
    private bool isRotated = false;

    void Start()
    {
        // Cache the original transform at start
        originalPosition = Camera.main.transform.position;
        originalRotationEuler = Camera.main.transform.eulerAngles;
    }

    public void RotateCamera180()
    {
        // --- NEW: Force the player to drop whatever they are holding! ---
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
        }
        else
        {
            Camera.main.transform.position = originalPosition;
            Camera.main.transform.eulerAngles = originalRotationEuler;
            isRotated = false;
            
            CameraSwipeMover.Instance.currentInput = 1;
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

    void Awake()
    {
        Instance = this;
    }

    public void Serve()
    {
        if (currentCustomer != null && stack != null)
        {
            currentCustomer.ReceiveOrder(stack);
        }
        
        RotateCamera180();
    }
}