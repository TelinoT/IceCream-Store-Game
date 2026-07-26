using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradesPanel;
    private RectTransform upgradesPanelRect; 
    public Transform scrollViewContent;
    public GameObject upgradeButtonPrefab;

    [Header("Camera References")]
    public Camera mainCamera;
    public Camera upgradeCameraTarget; 
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private float originalCamFOV;

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public Vector2 offScreenBottom = new Vector2(0f, -1500f);
    public float popDuration = 0.25f; // For the 3D preview bounce
    public float popOvershoot = 1.1f;

    private Vector2 originalUIPosition;
    private Coroutine currentAnim;
    private bool isAnimating = false;

    private UpgradeCategory currentCategory = UpgradeCategory.Perk;
    
    private IngredientDispenser currentlyPreviewedDispenser = null;
    
    private Coroutine popAnim;
    private Vector3 savedPreviewScale;
    private Vector3 savedPreviewPosition; 

    void Start()
    {
        if (upgradesPanelRect == null && upgradesPanel != null)
        {
            upgradesPanelRect = upgradesPanel.GetComponent<RectTransform>();
        }

        if (upgradesPanelRect != null)
        {
            originalUIPosition = upgradesPanelRect.anchoredPosition;
        }

        if (upgradesPanel != null) upgradesPanel.SetActive(false);
    }

    public void OpenUpgradesMenu()
    {
        if (isAnimating) return; 
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.HideUI();

        AudioManager.Instance.Play("ButtonPop");
        upgradesPanel.SetActive(true);
        
        ShowCategory(UpgradeCategory.Perk); 

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideIn());
    }

    public void CloseUpgradesMenu()
    {
        if (isAnimating) return; 

        AudioManager.Instance.Play("ButtonPop");
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.ShowUI();

        // --- NEW: Clear the preview if we close the menu without buying! ---
        if (currentlyPreviewedDispenser != null)
        {
            if (popAnim != null) StopCoroutine(popAnim);
            currentlyPreviewedDispenser.transform.localScale = savedPreviewScale;
            currentlyPreviewedDispenser.transform.localPosition = savedPreviewPosition; // --- NEW ---
            currentlyPreviewedDispenser.CheckUnlockState();
            currentlyPreviewedDispenser = null;
        }

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideOut());
    }

    public void ShowPerks() => ShowCategory(UpgradeCategory.Perk);
    public void ShowFlavors() => ShowCategory(UpgradeCategory.Flavor);
    public void ShowToppings() => ShowCategory(UpgradeCategory.Topping);

    private void ShowCategory(UpgradeCategory category)
    {
        currentCategory = category;
        AudioManager.Instance.Play("ButtonPop");
        
        // Hide previous preview when switching tabs
        if (currentlyPreviewedDispenser != null)
        {
            if (popAnim != null) StopCoroutine(popAnim);
            currentlyPreviewedDispenser.transform.localScale = savedPreviewScale;
            currentlyPreviewedDispenser.transform.localPosition = savedPreviewPosition; // --- NEW ---
            currentlyPreviewedDispenser.CheckUnlockState();
            currentlyPreviewedDispenser = null;
        }
        
        PopulateUpgrades();
    }

    // --- ANIMATION COROUTINES WITH CAMERA LOGIC ---

    private IEnumerator SlideIn()
    {
        isAnimating = true;
        upgradesPanelRect.anchoredPosition = offScreenBottom;

        // --- NEW: Save the exact state of the Night Hub camera ---
        if (mainCamera != null)
        {
            originalCamPos = mainCamera.transform.position;
            originalCamRot = mainCamera.transform.rotation;
            originalCamFOV = mainCamera.fieldOfView;
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / slideDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            upgradesPanelRect.anchoredPosition = Vector2.Lerp(offScreenBottom, originalUIPosition, easeT);
            
            // --- NEW: Glide Camera to Kitchen Aisle ---
            if (mainCamera != null && upgradeCameraTarget != null)
            {
                mainCamera.transform.position = Vector3.Lerp(originalCamPos, upgradeCameraTarget.transform.position, easeT);
                mainCamera.transform.rotation = Quaternion.Lerp(originalCamRot, upgradeCameraTarget.transform.rotation, easeT);
                mainCamera.fieldOfView = Mathf.Lerp(originalCamFOV, upgradeCameraTarget.fieldOfView, easeT);
            }

            yield return null;
        }

        upgradesPanelRect.anchoredPosition = originalUIPosition;
        
        if (mainCamera != null && upgradeCameraTarget != null)
        {
            mainCamera.transform.position = upgradeCameraTarget.transform.position;
            mainCamera.transform.rotation = upgradeCameraTarget.transform.rotation;
            mainCamera.fieldOfView = upgradeCameraTarget.fieldOfView;
        }
        
        isAnimating = false;
    }

    private IEnumerator SlideOut()
    {
        isAnimating = true;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / slideDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            upgradesPanelRect.anchoredPosition = Vector2.Lerp(originalUIPosition, offScreenBottom, easeT);
            
            // --- NEW: Glide Camera back to Night Hub ---
            if (mainCamera != null && upgradeCameraTarget != null)
            {
                mainCamera.transform.position = Vector3.Lerp(upgradeCameraTarget.transform.position, originalCamPos, easeT);
                mainCamera.transform.rotation = Quaternion.Lerp(upgradeCameraTarget.transform.rotation, originalCamRot, easeT);
                mainCamera.fieldOfView = Mathf.Lerp(upgradeCameraTarget.fieldOfView, originalCamFOV, easeT);
            }

            yield return null;
        }

        upgradesPanelRect.anchoredPosition = offScreenBottom;
        
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCamPos;
            mainCamera.transform.rotation = originalCamRot;
            mainCamera.fieldOfView = originalCamFOV;
        }
        
        upgradesPanel.SetActive(false);
        isAnimating = false;

        if (DayManager.Instance != null)
        {
            DayManager.Instance.ShowNightHubPanel();
        }
    }
    
    private void PopulateUpgrades()
    {
        foreach (Transform child in scrollViewContent)
        {
            Destroy(child.gameObject);
        }

        foreach (UpgradeData upgrade in UpgradeManager.Instance.allUpgrades.Where(u => u.category == currentCategory))
        {
            GameObject btnObj = Instantiate(upgradeButtonPrefab, scrollViewContent);
            GameObject child = btnObj.transform.GetChild(0).gameObject;
            
            TMP_Text nameText = child.transform.Find("NameText").GetComponent<TMP_Text>();
            TMP_Text descText = child.transform.Find("DescText").GetComponent<TMP_Text>();
            TMP_Text levelText = child.transform.Find("LevelText").GetComponent<TMP_Text>();
            Button buyBtn = child.transform.Find("BuyButton").GetComponent<Button>();
            TMP_Text costText = buyBtn.transform.GetChild(0).GetComponent<TMP_Text>();

            Transform iconTransform = child.transform.Find("IconImage");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (upgrade.icon != null)
                {
                    iconImage.sprite = upgrade.icon;
                    iconImage.enabled = true;
                }
            }

            nameText.text = upgrade.displayName;
            descText.text = upgrade.description;

            UpdateButtonUI(upgrade, levelText, costText, buyBtn);

            // --- NEW: Add Preview Listener to the main button background ---
            Button mainBtn = btnObj.GetComponent<Button>();
            if (mainBtn != null) 
            {
                mainBtn.onClick.AddListener(() => PreviewUpgrade(upgrade));
            }

            buyBtn.onClick.AddListener(() =>
            {
                if (UpgradeManager.Instance.TryBuyUpgrade(upgrade))
                {
                    UpdateButtonUI(upgrade, levelText, costText, buyBtn);
                    
                    // Force the previewed item to stay forever now that it's owned
                    if (currentlyPreviewedDispenser != null && currentlyPreviewedDispenser.ingredient.unlockID == upgrade.upgradeID)
                    {
                        currentlyPreviewedDispenser = null; 
                    }
                }
                else
                {
                    AudioManager.Instance.Play("Fail");
                }
            });
        }
    }

    // --- NEW: The Preview System ---
    private void PreviewUpgrade(UpgradeData upgrade)
    {
        AudioManager.Instance.Play("ButtonPop");

        if (currentlyPreviewedDispenser != null)
        {
            if (popAnim != null) StopCoroutine(popAnim);
            currentlyPreviewedDispenser.transform.localScale = savedPreviewScale;
            currentlyPreviewedDispenser.transform.localPosition = savedPreviewPosition; // --- NEW ---
            currentlyPreviewedDispenser.CheckUnlockState();
        }

        IngredientDispenser[] allDispensers = FindObjectsOfType<IngredientDispenser>(true);
        IngredientDispenser targetDispenser = allDispensers.FirstOrDefault(d => 
            d.ingredient != null && d.ingredient.unlockID == upgrade.upgradeID);

        if (targetDispenser != null)
        {
            currentlyPreviewedDispenser = targetDispenser;
            savedPreviewScale = targetDispenser.transform.localScale;
            savedPreviewPosition = targetDispenser.transform.localPosition; // --- NEW ---
            
            targetDispenser.gameObject.SetActive(true);

            if (popAnim != null) StopCoroutine(popAnim);
            
            // --- CHANGED: Call your new coroutine ---
            popAnim = StartCoroutine(CleanScalePop(targetDispenser.transform, savedPreviewScale, savedPreviewPosition)); 
        }
    }

    private void UpdateButtonUI(UpgradeData upgrade, TMP_Text levelText, TMP_Text costText, Button buyBtn)
    {
        int currentLevel = UpgradeManager.Instance.GetUpgradeLevel(upgrade.upgradeID);
        int playerLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 1;
        
        if (playerLevel < upgrade.requiredLevel)
        {
            levelText.text = "LOCKED";
            costText.text = $"Unlocks Lvl {upgrade.requiredLevel}";
            buyBtn.interactable = false;
            return;
        }

        if (upgrade.isOneTimeUnlock)
        {
            levelText.text = currentLevel >= 1 ? "OWNED" : "UNOWNED";
            
            if (currentLevel >= 1)
            {
                costText.text = "BOUGHT";
                buyBtn.interactable = false;
            }
            else
            {
                costText.text = $"Buy: {upgrade.baseCost}$";
                buyBtn.interactable = PlayerPrefs.GetInt("Coins", 0) >= upgrade.baseCost;
            }
        }
        else
        {
            levelText.text = $"{currentLevel} / {upgrade.maxLevel}";
            
            if (currentLevel >= upgrade.maxLevel)
            {
                costText.text = "MAXED";
                buyBtn.interactable = false;
            }
            else
            {
                int cost = UpgradeManager.Instance.GetNextLevelCost(upgrade);
                costText.text = $"Buy: {cost}$";
                buyBtn.interactable = PlayerPrefs.GetInt("Coins", 0) >= cost;
            }
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
            
            // Force the position to stay anchored exactly where it belongs
            target.localPosition = basePosition; 
            
            yield return null;
        }
        
        target.localScale = baseScale;
        target.localPosition = basePosition; 
    }
}