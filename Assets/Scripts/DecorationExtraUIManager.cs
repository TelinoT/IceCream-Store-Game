using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DecorationExtraUIManager : MonoBehaviour
{
    public Transform scrollViewContent;
    public Button buttonPrefab;
    public DecorationCategory currentCategory;

    public DecorationObject[] allDecorations;

    private DecorationObject currentlySelected = null;
    private GameObject currentBuyButton = null;
    private GameObject currentToggleButton = null;
    
    private Dictionary<string, Vector3> originalScales = new Dictionary<string, Vector3>();
    private Dictionary<string, Vector3> originalPositions = new Dictionary<string, Vector3>();
    
    private Coroutine activePopCoroutine = null;
    private DecorationObject lastAnimatedDeco = null;
    
    public void Setup()
    {
        allDecorations = FindObjectsOfType<DecorationObject>(true);
        
        foreach (var deco in allDecorations)
        {
            if (!originalScales.ContainsKey(deco.DecorationID))
            {
                originalScales.Add(deco.DecorationID, deco.transform.localScale);
                originalPositions.Add(deco.DecorationID, deco.transform.localPosition);
            }
        }
        
        ShowCategory(currentCategory);
    }

    public void ShowWallLeft()  => ShowCategory(DecorationCategory.WallLeft);
    public void ShowCounter()   => ShowCategory(DecorationCategory.Counter);
    public void ShowWallRight() => ShowCategory(DecorationCategory.WallRight);
    public void ShowWallFront() => ShowCategory(DecorationCategory.WallFront);
    public void ShowFloor()     => ShowCategory(DecorationCategory.Floor);
    public void ShowTables()    => ShowCategory(DecorationCategory.Tables);

    public void ShowCategory(DecorationCategory category)
    {
        currentCategory = category;
        currentlySelected = null;
        ClearScrollView();

foreach (var deco in allDecorations)
        {
            if (deco.category != category) continue;

            // --- PERK: Interior Designer ---
            int discount = Mathf.RoundToInt(UpgradeManager.Instance.GetCurrentStatValueByID("deco_discount"));
            int actualPrice = Mathf.Max(0, deco.price - discount); 

            var btn = Instantiate(buttonPrefab, scrollViewContent);
            TMP_Text mainBtnText = btn.transform.GetChild(2).GetComponent<TMP_Text>();
            mainBtnText.text = deco.name;

            Transform buyBtnTransform = btn.transform.GetChild(0);
            Transform toggleBtnTransform = btn.transform.GetChild(1);
            Button buyBtn = buyBtnTransform.GetComponent<Button>();
            TMP_Text buyBtnText = buyBtnTransform.GetComponentInChildren<TMP_Text>();
            Button toggleBtn = toggleBtnTransform.GetComponent<Button>();
            TMP_Text toggleBtnText = toggleBtnTransform.GetComponentInChildren<TMP_Text>();

            buyBtn.gameObject.SetActive(false);
            toggleBtn.gameObject.SetActive(false);

            btn.onClick.AddListener(() =>
            {
                if (currentlySelected != null && currentlySelected != deco)
                {
                    currentBuyButton?.SetActive(false);
                    currentToggleButton?.SetActive(false);
                }
                
                AudioManager.Instance.Play("ButtonPop");
                currentlySelected = deco;
                currentBuyButton = buyBtn.gameObject;
                currentToggleButton = toggleBtn.gameObject;
                
                HideUnownedSceneDecorations();
                deco.Preview(true);
                
                if (activePopCoroutine != null) 
                {
                    StopCoroutine(activePopCoroutine);
                }
                
                if (lastAnimatedDeco != null && lastAnimatedDeco != deco)
                {
                    // Snap the previously clicked item back to safety
                    lastAnimatedDeco.transform.localScale = originalScales[lastAnimatedDeco.DecorationID];
                    lastAnimatedDeco.transform.localPosition = originalPositions[lastAnimatedDeco.DecorationID];
                }

                lastAnimatedDeco = deco;
                activePopCoroutine = StartCoroutine(CleanScalePop(deco.transform, originalScales[deco.DecorationID], originalPositions[deco.DecorationID]));
                
                if (!deco.IsBought())
                {
                    // --- APPLY DISCOUNTED PRICE HERE ---
                    buyBtnText.text = $"Buy: {actualPrice}$";
                    buyBtn.gameObject.SetActive(true);
                    toggleBtn.gameObject.SetActive(false);
                }
                else
                {
                    buyBtn.gameObject.SetActive(false);
                    toggleBtn.gameObject.SetActive(true);
                    toggleBtnText.text = deco.IsActive() ? "Deactivate" : "Activate";
                }
                
                int coins = PlayerPrefs.GetInt("Coins", 0); 
                // --- APPLY DISCOUNTED PRICE HERE ---
                buyBtn.interactable = coins >= actualPrice;
            });
            
            int coins = PlayerPrefs.GetInt("Coins", 0); 
            // --- APPLY DISCOUNTED PRICE HERE ---
            buyBtn.interactable = coins >= actualPrice;

            buyBtn.onClick.AddListener(() =>
            {
                // --- APPLY DISCOUNTED PRICE HERE ---
                if (EconomyManager.Instance.TrySpendCoins(actualPrice))
                {
                    deco.Buy();
                    buyBtn.gameObject.SetActive(false);
                    toggleBtn.gameObject.SetActive(true);
                    toggleBtnText.text = "Deactivate";
                    AudioManager.Instance.Play("BuyButton");
                }
                else
                {
                    buyBtn.interactable = false;
                }
            });

            toggleBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance.Play("ButtonPop");
                deco.Activate();
                toggleBtnText.text = deco.IsActive() ? "Deactivate" : "Activate";
            });
        }
    }
    
    public void HideUnownedSceneDecorations()
    {
        foreach (var deco in allDecorations)
        {
            bool bought = deco.IsBought(); 
            bool isActive = deco.gameObject.activeSelf;

            // Hide it if active in scene but not bought
            if (!bought && isActive)
            {
                deco.gameObject.SetActive(false);
            }
        }
    }

    private void ClearScrollView()
    {
        currentlySelected = null;
        currentBuyButton = null;
        currentToggleButton = null;

        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);
    }

    public void ExitShop()
    {
        if (currentBuyButton != null) currentBuyButton.SetActive(false);
        if (currentToggleButton != null) currentToggleButton.SetActive(false);

        currentlySelected = null;
        currentBuyButton = null;
        currentToggleButton = null;
        
        if (allDecorations != null)
        {
            HideUnownedSceneDecorations();
        }
    }
    
    // --- UPDATED: The Heartbeat Pulse Coroutine ---
    /*private IEnumerator JellyPop(Transform target, Vector3 targetScale)
    {
        float duration = 0.3f; // Snappy and quick
        float elapsed = 0f;
        
        // Immediately make sure it is visible and at the correct starting scale
        target.localScale = targetScale;

        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so it plays smoothly while the shop is paused
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;
            
            // Mathf.Sin creates a perfect curve that goes from 0, peaks at 1, and goes back to 0.
            // We multiply by 0.15f to give it a 15% size boost at the peak of the pulse.
            float popMultiplier = 1f + (Mathf.Sin(t * Mathf.PI) * 0.15f);

            // Apply the safe, relative scale
            target.localScale = targetScale * popMultiplier;
            yield return null;
        }
        
        // Guarantee it perfectly snaps back to the exact original scale
        target.localScale = targetScale;
    }*/
    
    /*private IEnumerator JellyPop(Transform target, Vector3 baseScale)
    {
        float duration = 0.3f; // Quick and snappy
        float elapsed = 0f;
        
        // Target a peak size 15% larger than normal
        Vector3 peakScale = baseScale * 1.15f; 

        while (elapsed < duration)
        {
            // Use unscaledDeltaTime to ignore the paused shop menu
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;
            
            // Mathf.Sin creates a perfect curve going from 0 to 1 and back to 0
            float curve = Mathf.Sin(t * Mathf.PI);

            // Smoothly Lerp between the base scale and the peak scale
            target.localScale = Vector3.Lerp(baseScale, peakScale, curve);
            
            yield return null;
        }
        
        // Guarantee it snaps exactly back to normal at the end
        target.localScale = baseScale;
    }*/
    
    private IEnumerator CleanScalePop(Transform target, Vector3 baseScale, Vector3 basePosition)
    {
        float duration = 0.3f; 
        float elapsed = 0f;
        Vector3 peakScale = baseScale * 1.15f; 

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI);

            target.localScale = Vector3.Lerp(baseScale, peakScale, curve);
            
            // Force the position to stay anchored exactly where it belongs
            target.localPosition = basePosition; 
            
            yield return null;
        }
        
        target.localScale = baseScale;
        target.localPosition = basePosition; 
    }
}
