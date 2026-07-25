using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradesPanel;
    private RectTransform upgradesPanelRect; // The RectTransform of the panel to slide
    public Transform scrollViewContent;
    public GameObject upgradeButtonPrefab;

    [Header("Animation Settings")]
    public float slideDuration = 0.4f;
    public Vector2 offScreenBottom = new Vector2(0f, -1500f);

    private Vector2 originalUIPosition;
    private Coroutine currentAnim;
    private bool isAnimating = false;

    void Start()
    {
        // Automatically grab the RectTransform if you forgot to drag it in
        if (upgradesPanelRect == null && upgradesPanel != null)
        {
            upgradesPanelRect = upgradesPanel.GetComponent<RectTransform>();
        }

        // Save where you placed it in the Editor!
        if (upgradesPanelRect != null)
        {
            originalUIPosition = upgradesPanelRect.anchoredPosition;
        }

        if (upgradesPanel != null) upgradesPanel.SetActive(false);
    }

    public void OpenUpgradesMenu()
    {
        if (isAnimating) return; // Prevent double-clicking
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.HideUI();

        AudioManager.Instance.Play("ButtonPop");
        upgradesPanel.SetActive(true);
        PopulateUpgrades();

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideIn());
    }

    public void CloseUpgradesMenu()
    {
        if (isAnimating) return; // Prevent double-clicking

        AudioManager.Instance.Play("ButtonPop");
        
        if (PersistentUIController.Instance != null) PersistentUIController.Instance.ShowUI();

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(SlideOut());
    }

    // --- ANIMATION COROUTINES ---

    private IEnumerator SlideIn()
    {
        isAnimating = true;
        
        // Snap to the bottom before we start sliding
        upgradesPanelRect.anchoredPosition = offScreenBottom;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / slideDuration;
            
            // Ease-out curve (fast start, smooth stop)
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            upgradesPanelRect.anchoredPosition = Vector2.Lerp(offScreenBottom, originalUIPosition, easeT);
            yield return null;
        }

        // Snap exactly to the final position
        upgradesPanelRect.anchoredPosition = originalUIPosition;
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
            
            // Ease-out curve
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            upgradesPanelRect.anchoredPosition = Vector2.Lerp(originalUIPosition, offScreenBottom, easeT);
            yield return null;
        }

        upgradesPanelRect.anchoredPosition = offScreenBottom;
        upgradesPanel.SetActive(false);
        isAnimating = false;

        // Return to Night Hub only after the animation finishes sliding down!
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

        foreach (UpgradeData upgrade in UpgradeManager.Instance.allUpgrades)
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
            // ------------------------------------------

            nameText.text = upgrade.displayName;
            descText.text = upgrade.description;

            UpdateButtonUI(upgrade, levelText, costText, buyBtn);

            buyBtn.onClick.AddListener(() =>
            {
                if (UpgradeManager.Instance.TryBuyUpgrade(upgrade))
                {
                    UpdateButtonUI(upgrade, levelText, costText, buyBtn);
                }
                else
                {
                    AudioManager.Instance.Play("Fail");
                }
            });
        }
    }

    private void UpdateButtonUI(UpgradeData upgrade, TMP_Text levelText, TMP_Text costText, Button buyBtn)
    {
        int currentLevel = UpgradeManager.Instance.GetUpgradeLevel(upgrade.upgradeID);
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