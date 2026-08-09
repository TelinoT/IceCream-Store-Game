using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IngredientDispenser : MonoBehaviour
{
    [Header("Base Settings")]
    public IceCreamIngredient ingredient;
    public GameObject draggableBasePrefab;
    public bool isCookie;
    public bool hideWhileDragging = false; 
    
    [Header("Visuals")]
    public GameObject visualModel; // Used for hiding during drag AND sinking when empty

    [Header("Depletion Settings")]
    public bool usesDepletion = false; // Check this ONLY for Ice Cream Flavors!
    public int maxScoops = 8;
    public float emptyYOffset = -0.4f; // How far down (in local space) it sinks
    public float refillDuration = 2.0f;
    public float sinkDuration = 0.25f;
    public float depletionCurve = 0.7f;

    [Header("Refill UI")]
    public GameObject refillCanvas;
    public GameObject refillButton;
    public GameObject progressBarContainer;
    private Slider progressSlider;

    private int currentScoops;
    private Vector3 originalLocalPos;
    private Vector3 emptyLocalPos;
    
    private Coroutine sinkCoroutine; // Tracks the animation so fast scooping doesn't glitch

    // Public properties for the Input Manager to read
    public bool IsEmpty => usesDepletion && currentScoops <= 0;
    public bool IsRefilling { get; private set; } = false;

    public void SetVisuals(bool state)
    {
        if (hideWhileDragging && visualModel != null)
        {
            visualModel.SetActive(state);
        }
    }
    
    void Start()
    {
        CheckUnlockState();
        
        progressSlider = progressBarContainer.GetComponent<Slider>();

        if (usesDepletion)
        {
            currentScoops = maxScoops;
            
            if (visualModel != null)
            {
                originalLocalPos = visualModel.transform.localPosition;
                emptyLocalPos = originalLocalPos + new Vector3(0f, emptyYOffset, 0f);
            }
            
            LoadScoops();
            
            if (visualModel != null)
            {
                // --- FIX: Use the new curve helper! ---
                float fillRatio = GetVisualFillRatio(currentScoops);
                visualModel.transform.localPosition = Vector3.Lerp(emptyLocalPos, originalLocalPos, fillRatio);
            }

            if (refillCanvas != null) refillCanvas.SetActive(false);
            
            if (currentScoops <= 0)
            {
                ShowRefillUI();
            }
        }
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
        // Safety check to prevent spawning if empty!
        if (IsEmpty || IsRefilling) return null;

        Vector3 spawnPos = this.transform.position;

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

    // --- DEPLETION & REFILL LOGIC ---

    public void ConsumeScoop()
    {
        if (!usesDepletion || IsEmpty || IsRefilling) return;

        currentScoops--;
        SaveScoops(); // Save the new count immediately!

        // Stop the old animation if the player is carving lightning fast, and start a new one!
        if (sinkCoroutine != null) StopCoroutine(sinkCoroutine);
        sinkCoroutine = StartCoroutine(SmoothSinkRoutine());

        if (currentScoops <= 0)
        {
            ShowRefillUI();
        }
    }

    private void UpdateVisualHeight()
    {
        if (visualModel == null) return;
        
        float fillRatio = (float)currentScoops / maxScoops;
        visualModel.transform.localPosition = Vector3.Lerp(emptyLocalPos, originalLocalPos, fillRatio);
    }

    private void ShowRefillUI()
    {
        if (refillCanvas != null)
        {
            refillCanvas.SetActive(true);
            if (refillButton != null) refillButton.SetActive(true);
            if (progressBarContainer != null) progressBarContainer.SetActive(false);
        }
    }

    public void StartRefill()
    {
        if (!IsEmpty || IsRefilling) return;
        StartCoroutine(RefillRoutine());
    }

    private IEnumerator RefillRoutine()
    {
        IsRefilling = true;
        AudioManager.Instance.Play("ButtonPop"); 
        
        // Swap to Progress Bar
        if (refillButton != null) refillButton.SetActive(false);
        if (progressBarContainer != null) 
        {
            progressBarContainer.SetActive(true);
            if (progressSlider != null) progressSlider.value = 0f;
        }

        float elapsed = 0f;
        while (elapsed < refillDuration)
        {
            elapsed += Time.deltaTime; 
            float t = elapsed / refillDuration;

            if (progressSlider != null) progressSlider.value = t;
            
            if (visualModel != null)
            {
                visualModel.transform.localPosition = Vector3.Lerp(emptyLocalPos, originalLocalPos, t);
            }

            yield return null;
        }

        // Lock in final values
        currentScoops = maxScoops;
        
        SaveScoops(); // --- FIX 2: Save the fact that we just refilled! ---
        
        if (visualModel != null) visualModel.transform.localPosition = originalLocalPos;
        if (refillCanvas != null) refillCanvas.SetActive(false);

        AudioManager.Instance.Play("Success"); 
        IsRefilling = false;
    }
    
    private IEnumerator SmoothSinkRoutine()
    {
        if (visualModel == null) yield break;

        // --- FIX: Use the new curve helper! ---
        float fillRatio = GetVisualFillRatio(currentScoops);
        Vector3 targetPos = Vector3.Lerp(emptyLocalPos, originalLocalPos, fillRatio);
        Vector3 startPos = visualModel.transform.localPosition;

        float elapsed = 0f;
        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sinkDuration;
            
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            visualModel.transform.localPosition = Vector3.Lerp(startPos, targetPos, easeT);
            yield return null;
        }

        visualModel.transform.localPosition = targetPos;
    }
    
    private string GetSaveKey()
    {
        // Creates a unique key per flavor, like "ScoopsLeft_Vanilla" or "ScoopsLeft_Strawberry"
        if (ingredient != null)
        {
            return "ScoopsLeft_" + ingredient.name;
        }
        return "ScoopsLeft_UnknownDispenser"; 
    }

    private void SaveScoops()
    {
        PlayerPrefs.SetInt(GetSaveKey(), currentScoops);
        PlayerPrefs.Save();
    }

    private void LoadScoops()
    {
        // Loads the saved amount, but defaults to 'maxScoops' if they've never played before!
        currentScoops = PlayerPrefs.GetInt(GetSaveKey(), maxScoops);
    }
    
    private float GetVisualFillRatio(int scoops)
    {
        // Get the linear percentage (e.g., 4 / 8 = 0.5)
        float linearRatio = (float)scoops / maxScoops;
        
        // Apply the curve! (Using 0.5 acts like a Square Root)
        return Mathf.Pow(linearRatio, depletionCurve);
    }
}