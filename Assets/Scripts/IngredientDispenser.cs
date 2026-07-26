using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class IngredientDispenser : MonoBehaviour
{
    public IceCreamIngredient ingredient;
    public GameObject draggableBasePrefab;

    public DraggableBase currentDraggedBase = null;
    private bool isDragging = false;

    public bool isCookie;
    
    void Start()
    {
        CheckUnlockState();
    }
    
    public void CheckUnlockState()
    {
        if (ingredient != null && !string.IsNullOrEmpty(ingredient.unlockID))
        {
            if (UpgradeManager.Instance.GetUpgradeLevel(ingredient.unlockID) < 1)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }
    }
    
    public DraggableBase SpawnIngredient()
    {
        Vector3 spawnPos = this.transform.position;
        // spawnPos.y = 0.1f; // Optional: Keep your original height logic if needed

        GameObject newBaseObj = Instantiate(draggableBasePrefab, spawnPos, Quaternion.identity);
        DraggableBase newBase = newBaseObj.GetComponent<DraggableBase>();

        if (ingredient.type == IngredientType.Flavor)
        {
            AudioManager.Instance.Play("ScoopPickUp");
        }

        if (isCookie)
        {
            AudioManager.Instance.Play("ConeGrab");
        }
        
        if (!isCookie && ingredient.type == IngredientType.Base)
        {
            AudioManager.Instance.Play("CupGrab");
        }

        if (ingredient.type == IngredientType.Topping)
        {
            AudioManager.Instance.Play("SprinklesTake");
        }
        
        newBase.Initialize(ingredient);
        
        return newBase;
    }

    /*void Update()
    {
        if (IceCreamStack.Instance.HasBase())
        {
            if (ingredient.type == IngredientType.Flavor  || ingredient.type == IngredientType.Topping)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // Raycast to check if dispenser was clicked
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        if (hit.collider.gameObject == gameObject)
                        {
                            isDragging = true;
                        }
                    }
                }

                if (isDragging && Input.GetMouseButton(0))
                {
                    if (currentDraggedBase == null)
                    {
                        Vector3 spawnPos = this.gameObject.transform.position;
                        spawnPos.y = 0.1f; // initial spawn height, won't matter much now

                        GameObject newBase = Instantiate(draggableBasePrefab, spawnPos, Quaternion.identity);
                        currentDraggedBase = newBase.GetComponent<DraggableBase>();
                        currentDraggedBase.Initialize(ingredient);
                    }

                    // Pass mouse position (screen position) so DraggableBase can raycast
                    currentDraggedBase.MoveTo(Input.mousePosition);
                }

                if (isDragging && Input.GetMouseButtonUp(0))
                {
                    isDragging = false;

                    if (currentDraggedBase != null)
                    {
                        currentDraggedBase.TryPlace();
                        currentDraggedBase = null;
                    }
                }
            }
        }
        else
        {
            if (ingredient.type != IngredientType.Base) return;

            if (Input.GetMouseButtonDown(0))
            {
                // Raycast to check if dispenser was clicked
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        isDragging = true;
                    }
                }
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                if (currentDraggedBase == null)
                {
                    Vector3 spawnPos = this.gameObject.transform.position;
                    spawnPos.y = 0.1f; // initial spawn height, won't matter much now

                    GameObject newBase = Instantiate(draggableBasePrefab, spawnPos, Quaternion.identity);
                    currentDraggedBase = newBase.GetComponent<DraggableBase>();
                    currentDraggedBase.Initialize(ingredient);
                }

                // Pass mouse position (screen position) so DraggableBase can raycast
                currentDraggedBase.MoveTo(Input.mousePosition);
            }

            if (isDragging && Input.GetMouseButtonUp(0))
            {
                isDragging = false;

                if (currentDraggedBase != null)
                {
                    currentDraggedBase.TryPlace();
                    currentDraggedBase = null;
                }
            }
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.point;
        }
        return Vector3.zero;
    }*/
}
