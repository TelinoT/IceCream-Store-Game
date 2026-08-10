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
    
    // Tracking variables for original layout
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
            
            Image iconImage = btn.transform.Find("IconImage").GetComponent<Image>(); 
            if (deco.uiIcon != null)
            {
                iconImage.sprite = deco.uiIcon;
            }

            // --- NEW: Evaluate and show button states IMMEDIATELY upon creating the card ---
            if (!deco.IsBought())
            {
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
            buyBtn.interactable = coins >= actualPrice;

            // Clicking the main card now ONLY handles previewing and the 3D popping animation
            btn.onClick.AddListener(() =>
            {
                AudioManager.Instance.Play("ButtonPop");
                currentlySelected = deco;
                
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
            });

            buyBtn.onClick.AddListener(() =>
            {
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

            if (!bought && isActive)
            {
                deco.gameObject.SetActive(false);
            }
        }
    }

    private void ClearScrollView()
    {
        currentlySelected = null;
        
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);
    }

    public void ExitShop()
    {
        currentlySelected = null;
        
        if (allDecorations != null)
        {
            HideUnownedSceneDecorations();
        }
    }
    
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
            target.localPosition = basePosition; 
            
            yield return null;
        }
        
        target.localScale = baseScale;
        target.localPosition = basePosition; 
    }
}