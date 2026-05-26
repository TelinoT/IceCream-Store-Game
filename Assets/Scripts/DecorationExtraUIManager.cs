using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DecorationExtraUIManager : MonoBehaviour
{
    public Transform scrollViewContent;
    public Button buttonPrefab;
    public DecorationCategory currentCategory;

    public DecorationObject[] allDecorations;

    private DecorationObject currentlySelected = null;
    private GameObject currentBuyButton = null;
    private GameObject currentToggleButton = null;

    public void Setup()
    {
        allDecorations = FindObjectsOfType<DecorationObject>(true);
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
}
