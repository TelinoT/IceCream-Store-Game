using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomerOrder : MonoBehaviour
{
    public IceCreamRecipe desiredRecipe;
    public TextMeshProUGUI speechText; 
    
    [Header("VIP Settings")]
    public bool isVIP = false;
    public VIPCharacterData vipData;
    public VIPEncounter currentEncounter;
    
    [Header("VIP Speech Bubble UI")]
    public TextMeshProUGUI nameText;
    public GameObject optionsContainer; 
    public Button optionA_Button;
    public TextMeshProUGUI optionA_Text;
    public Button optionB_Button;
    public TextMeshProUGUI optionB_Text;
    
    [Header("Patience & Tipping")]
    public float maxPatience = 30f;     
    public int maxTip = 3;              
    public Gradient patienceColorGradient; 

    [Header("Out of Patience Dialogue")]
    public string[] outOfPatienceLines = new string[] 
    {
        "I can't wait any longer!",
        "This is taking forever. I'm out!",
        "Forget it, I'm going somewhere else!"
    };

    private float currentPatience;
    private bool orderHandled = false;
    private bool isTimerActive = false;
    [HideInInspector] public bool isTimerPaused = false;
    private DialogueTypewriter typewriter;
    
    void Start()
    {
        float patienceBonus = UpgradeManager.Instance.GetCurrentStatValueByID("max_patience");
        currentPatience = maxPatience + patienceBonus;
        maxPatience = currentPatience;
        
        int tipBonus = Mathf.RoundToInt(UpgradeManager.Instance.GetCurrentStatValueByID("max_tip_up"));
        maxTip += tipBonus;
        
        typewriter = speechText.GetComponent<DialogueTypewriter>();
        if (optionsContainer != null) optionsContainer.SetActive(false);

        if (isVIP && currentEncounter != null)
        {
            StartCoroutine(nameHim());            
            StartCoroutine(PlayVIPDialogue());
        }
        else
        {
            string line = desiredRecipe.orderLines.Length > 0 ? desiredRecipe.orderLines[Random.Range(0, desiredRecipe.orderLines.Length)] : "I'd like an ice cream!";
            StartCoroutine(PlayOrderDialogue(line));
        }
    }

    private IEnumerator nameHim()
    {
        yield return new WaitForSeconds(0.01f);
        if (nameText != null) nameText.text = vipData.characterName;
    }
    
    // --- UPDATED: The Auto-Flowing In-Bubble Dialogue with Fades ---
    private IEnumerator PlayVIPDialogue()
    {
        // 1. Play Intro lines sequentially
        foreach (string line in currentEncounter.introLines)
        {
            typewriter.ShowDialogue(line);
            
            // Wait for typing to finish OR for a screen tap to skip it
            while (typewriter.isTyping)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    typewriter.SkipTyping();
                    yield return null; // Wait one frame so this click doesn't also skip the next line
                    break;
                }
                yield return null;
            }

            // Wait until the player clicks anywhere to proceed to the next line
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        }

        // 2. Show Options with a Smooth Fade
        if (!string.IsNullOrEmpty(currentEncounter.optionA_Text))
        {
            optionsContainer.SetActive(true);
            
            CanvasGroup cg = optionsContainer.GetComponent<CanvasGroup>();
            if (cg == null) cg = optionsContainer.AddComponent<CanvasGroup>();
            
            cg.alpha = 0f; 
            
            optionA_Text.text = currentEncounter.optionA_Text;
            optionB_Text.text = currentEncounter.optionB_Text;

            yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, 0.4f));

            bool choiceMade = false;
            bool choseA = false;

            optionA_Button.onClick.RemoveAllListeners();
            optionA_Button.onClick.AddListener(() => { choiceMade = true; choseA = true; });

            optionB_Button.onClick.RemoveAllListeners();
            optionB_Button.onClick.AddListener(() => { choiceMade = true; choseA = false; });

            // Wait for the player to click a UI button
            yield return new WaitUntil(() => choiceMade);
            
            AudioManager.Instance.Play("ButtonPop");

            yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, 0.2f));
            optionsContainer.SetActive(false);

            // Wait one frame so the UI button click doesn't accidentally skip the first response line!
            yield return null;

            // 3. Play Response lines sequentially
            List<string> responses = choseA ? currentEncounter.responseA_Lines : currentEncounter.responseB_Lines;
            foreach (string line in responses)
            {
                typewriter.ShowDialogue(line);
                
                // Wait for typing to finish OR for a screen tap to skip it
                while (typewriter.isTyping)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        typewriter.SkipTyping();
                        yield return null; 
                        break;
                    }
                    yield return null;
                }

                // Wait until the player clicks anywhere to proceed to the next line
                yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
            }
        }

        // 4. Finish Dialogue
        FinishDialoguePhase();
    }

    // --- NEW: The Smooth Fade Helper ---
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Add a slight ease-out curve for extra juice
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 
            
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, easeT);
            yield return null;
        }
        cg.alpha = endAlpha;
    }
    
    private void FinishDialoguePhase()
    {
        VIPManager.Instance.AssignVIPTask(vipData);
        
        if (!currentEncounter.requiresOrder)
        {
            EconomyManager.Instance.AddReward(0, 15, transform.position);
            AudioManager.Instance.Play("Success");
            StartCoroutine(HappyHop());
            StartCoroutine(LeaveWithoutOrder());
        }
        else
        {
            speechText.transform.parent.gameObject.SetActive(true);
            
            string line = desiredRecipe.orderLines.Length > 0 ? desiredRecipe.orderLines[Random.Range(0, desiredRecipe.orderLines.Length)] : "I'll take my usual!";
            
            // Use the new skippable routine instead of just showing the text!
            StartCoroutine(PlayOrderDialogue(line));
        }
    }
    
    private IEnumerator LeaveWithoutOrder()
    {
        orderHandled = true;
        yield return new WaitForSeconds(2.5f);
        DayManager.Instance.CustomerServed();
        Destroy(gameObject);
    }

    private IEnumerator StartPatienceTimer()
    {
        float extraDelay = UpgradeManager.Instance.GetCurrentStatValueByID("initial_delay");
        yield return new WaitForSeconds(1f + extraDelay);

        if (orderHandled) yield break;

        isTimerActive = true;

        if (PatienceUIManager.Instance != null)
        {
            PatienceUIManager.Instance.ShowSlider(maxPatience);
        }
    }

    void Update()
    {
        if (orderHandled || !isTimerActive || isTimerPaused) return;
        
        currentPatience -= Time.deltaTime;

        if (PatienceUIManager.Instance != null)
        {
            float patiencePercentage = currentPatience / maxPatience;
            Color newColor = patienceColorGradient.Evaluate(patiencePercentage);
            PatienceUIManager.Instance.UpdateSlider(currentPatience, newColor);
        }

        if (currentPatience <= 0)
        {
            StartCoroutine(LeaveAngry());
        }
    }

    public void ReceiveOrder(IceCreamStack stack)
    {
        if (orderHandled) return;
        orderHandled = true;
        
        if (Buttons.Instance != null)
        {
            Buttons.Instance.currentCustomer = null; 
            if (Buttons.Instance.serveUICanvas != null)
            {
                Buttons.Instance.serveUICanvas.SetActive(false);
            }
        }
        
        StartCoroutine(HandleOrderWithDelay(stack));
    }

    private IEnumerator HandleOrderWithDelay(IceCreamStack stack)
    {
        if (PatienceUIManager.Instance != null) PatienceUIManager.Instance.HideSlider();
        
        if (IceCreamStack.hasCone) TaskManager.Instance.ReportProgress(TaskGoalType.ServeCones, 1);

        bool correct = stack.MatchesRecipe(desiredRecipe);
        TaskManager.Instance.ReportProgress(TaskGoalType.ServeCustomers, 1);

        if (correct) TaskManager.Instance.ReportProgress(TaskGoalType.SellPerfectIceCream, 1);
        
        bool activatedSweetTalker = false;
        if (!correct)
        {
            float forgivenessChance = UpgradeManager.Instance.GetCurrentStatValueByID("wrong_order_chance");
            if (Random.Range(0f, 100f) < forgivenessChance)
            {
                correct = true; 
                activatedSweetTalker = true;
            }
        }

        if (correct)
        {
            float patiencePercentage = currentPatience / maxPatience;
            int earnedTip = Mathf.FloorToInt(patiencePercentage * maxTip);
            int perkBonus = Mathf.RoundToInt(UpgradeManager.Instance.GetCurrentStatValueByID("base_price_up"));
            int totalCash = desiredRecipe.price + earnedTip + perkBonus;
            
            float doubleChance = UpgradeManager.Instance.GetCurrentStatValueByID("double_pay");
            bool isDouble = Random.Range(0f, 100f) < doubleChance;
            if (isDouble) totalCash *= 2;
            
            int earnedXP = 5 + Mathf.FloorToInt(patiencePercentage * 10f);

            EconomyManager.Instance.AddReward(totalCash, earnedXP, transform.position);

            if (desiredRecipe.correctResponseLines.Length > 0)
            {
                string line = desiredRecipe.correctResponseLines[Random.Range(0, desiredRecipe.correctResponseLines.Length)];
                string fullLine = line + (earnedTip > 0 ? $" (+{earnedTip}$ Tip!)" : "");
                typewriter.ShowDialogue(fullLine);
            }
            
            if (activatedSweetTalker)
            {
                typewriter.ShowDialogue("Hmm, this isn't what I ordered, but it looks delicious! I'll take it.");
            }
            
            AudioManager.Instance.Play("Success");
            StartCoroutine(HappyHop());
        }
        else
        {
            if (desiredRecipe.wrongResponseLines.Length > 0)
            {
                string line = desiredRecipe.wrongResponseLines[Random.Range(0, desiredRecipe.wrongResponseLines.Length)];
                typewriter.ShowDialogue(line);            
            }
            AudioManager.Instance.Play("Fail");
            StartCoroutine(AngryShake());
        }

        yield return new WaitForSeconds(3f);
        stack.ResetStack();
        DayManager.Instance.CustomerServed();
        Destroy(gameObject); 
    }

    private IEnumerator LeaveAngry()
    {
        orderHandled = true;
        
        if (Buttons.Instance != null && Camera.main != null)
        {
            if (Vector3.Distance(Camera.main.transform.position, Buttons.Instance.targetPosition) < 0.1f)
            {
                Buttons.Instance.RotateCamera180(); 
                yield return new WaitForSeconds(0.3f);
            }
        }

        if (PatienceUIManager.Instance != null) 
        {
            PatienceUIManager.Instance.HideSlider();
            PatienceUIManager.Instance.SwitchServeUI(); 
        }

        if (outOfPatienceLines.Length > 0)
        {
            string line = outOfPatienceLines[Random.Range(0, outOfPatienceLines.Length)];
            typewriter.ShowDialogue(line);
        }
        
        AudioManager.Instance.Play("Fail");
        StartCoroutine(AngryShake());

        yield return new WaitForSeconds(3f);
        DayManager.Instance.CustomerServed(); 
        Destroy(gameObject);
    }
    
    IEnumerator HappyHop()
    {
        float yStart = transform.position.y;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float y = yStart + Mathf.Sin(t * Mathf.PI * 2) * 0.5f; 
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, yStart, transform.position.z);
    }

    IEnumerator AngryShake()
    {
        Vector3 startPos = transform.position;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float x = startPos.x + Mathf.Sin(t * 30f) * 0.2f; 
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
            yield return null;
        }
        transform.position = startPos;
    }
    
    public void RestorePatience(float amount)
    {
        if (orderHandled) return;
        currentPatience += amount;
        if (currentPatience > maxPatience) currentPatience = maxPatience;
    }
    
    private IEnumerator PlayOrderDialogue(string line)
    {
        typewriter.ShowDialogue(line);
        
        // Wait for typing to finish OR for a screen tap to skip it
        while (typewriter.isTyping)
        {
            if (Input.GetMouseButtonDown(0))
            {
                typewriter.SkipTyping();
                yield return null; 
                break;
            }
            yield return null;
        }

        // Only start the timer AFTER they finish talking!
        StartCoroutine(StartPatienceTimer());
    }
}