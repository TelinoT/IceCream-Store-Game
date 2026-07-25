using System.Collections;
using TMPro;
using UnityEngine;

public class CustomerOrder : MonoBehaviour
{
    public IceCreamRecipe desiredRecipe;
    public TextMeshProUGUI speechText; 
    
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
    
    void Start()
    {
        float patienceBonus = UpgradeManager.Instance.GetCurrentStatValueByID("max_patience");
        currentPatience = maxPatience + patienceBonus;
        maxPatience = currentPatience;
        
        int tipBonus = Mathf.RoundToInt(UpgradeManager.Instance.GetCurrentStatValueByID("max_tip_up"));
        maxTip += tipBonus;

        if (desiredRecipe.orderLines.Length > 0)
        {
            string line = desiredRecipe.orderLines[Random.Range(0, desiredRecipe.orderLines.Length)];
            speechText.text = line;
        }

        StartCoroutine(StartPatienceTimer());
    }

    // --- NEW: The Delay Coroutine ---
    private IEnumerator StartPatienceTimer()
    {
        float extraDelay = UpgradeManager.Instance.GetCurrentStatValueByID("initial_delay");
        yield return new WaitForSeconds(1f + extraDelay);
        //yield return new WaitForSeconds(1f);

        // Safety check: Did the player already serve them super fast? If so, stop here!
        if (orderHandled) yield break;

        // Turn on the timer math
        isTimerActive = true;

        // Tell the Main Canvas to show the slider
        if (PatienceUIManager.Instance != null)
        {
            PatienceUIManager.Instance.ShowSlider(maxPatience);
        }
    }

    void Update()
    {
        // --- UPDATED: Don't do any math if the timer hasn't started yet! ---
        if (orderHandled || !isTimerActive) return;

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
        
        if (IceCreamStack.hasCone)
        {
            TaskManager.Instance.ReportProgress(TaskGoalType.ServeCones, 1);
        }

        bool correct = stack.MatchesRecipe(desiredRecipe);
        
        TaskManager.Instance.ReportProgress(TaskGoalType.ServeCustomers, 1);

        if (correct)
        {
            TaskManager.Instance.ReportProgress(TaskGoalType.SellPerfectIceCream, 1);
        }
        
        bool activatedSweetTalker = false;
        
        if (!correct)
        {
            float forgivenessChance = UpgradeManager.Instance.GetCurrentStatValueByID("wrong_order_chance");
            if (Random.Range(0f, 100f) < forgivenessChance)
            {
                correct = true; // Override to true!
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
                speechText.text = line + (earnedTip > 0 ? $" (+{earnedTip}$ Tip!)" : "");
            }
            
            if (activatedSweetTalker)
            {
                speechText.text = "Hmm, this isn't what I ordered, but it looks delicious! I'll take it.";
            }
            
            AudioManager.Instance.Play("Success");
            StartCoroutine(HappyHop());
        }
        else
        {
            if (desiredRecipe.wrongResponseLines.Length > 0)
            {
                string line = desiredRecipe.wrongResponseLines[Random.Range(0, desiredRecipe.wrongResponseLines.Length)];
                speechText.text = line;
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
        
        // 1. FORCE THE CAMERA FIRST
        // Instead of checking the swipe input, we check if the camera is physically at the ingredient station!
        if (Buttons.Instance != null && Camera.main != null)
        {
            // If the camera is at the back counter target position...
            if (Vector3.Distance(Camera.main.transform.position, Buttons.Instance.targetPosition) < 0.1f)
            {
                Buttons.Instance.RotateCamera180(); // This will also cancel the drag!
                yield return new WaitForSeconds(0.3f);
            }
        }

        // 2. UPDATE THE UI
        if (PatienceUIManager.Instance != null) 
        {
            PatienceUIManager.Instance.HideSlider();
            PatienceUIManager.Instance.SwitchServeUI(); 
        }

        // 3. DO THE ANGRY ANIMATIONS
        if (outOfPatienceLines.Length > 0)
        {
            speechText.text = outOfPatienceLines[Random.Range(0, outOfPatienceLines.Length)];
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
}