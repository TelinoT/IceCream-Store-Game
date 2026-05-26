using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Animation Settings")]
    public float lifetime = 1.2f;       
    public float floatDistance = 1.5f;  

    private TextMeshProUGUI tmp;
    private Color initialColor;
    private float timer;
    private Vector3 startPos;
    
    // NEW: We remember YOUR specific scale (e.g. 0.01, 0.01, 0.01)
    private Vector3 initialScale; 

    void Awake()
    {
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        
        // 1. Capture the scale you set in the Inspector BEFORE we hide it
        initialScale = transform.localScale; 
        
        // 2. Hide it instantly so we can pop it in
        transform.localScale = Vector3.zero; 
    }

    void Start()
    {
        startPos = transform.position;
        if (tmp != null) initialColor = tmp.color;
        
        // Face camera immediately
        transform.rotation = Camera.main.transform.rotation;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // --- 1. MOVEMENT ---
        float height = Mathf.Sin(progress * Mathf.PI * 0.5f) * floatDistance;
        transform.position = startPos + Vector3.up * height;

        // --- 2. SCALE (Elastic Pop based on INITIAL SCALE) ---
        if (progress < 0.2f)
        {
            // Pop UP to 150% of YOUR size
            float scaleProgress = progress / 0.2f;
            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale * 1.5f, scaleProgress);
        }
        else if (progress < 0.4f)
        {
            // Shrink back to YOUR size
            float scaleProgress = (progress - 0.2f) / 0.2f;
            transform.localScale = Vector3.Lerp(initialScale * 1.5f, initialScale, scaleProgress);
        }
        else
        {
            // Stay at YOUR size
            transform.localScale = initialScale;
        }

        // --- 3. FADE OUT ---
        if (progress > 0.6f)
        {
            float fadeProgress = (progress - 0.6f) / 0.4f;
            if (tmp != null)
            {
                tmp.color = new Color(initialColor.r, initialColor.g, initialColor.b, 1f - fadeProgress);
            }
        }
    }

    public void SetText(string text, Color color)
    {
        if (tmp == null) tmp = GetComponentInChildren<TextMeshProUGUI>();
        
        if (tmp != null) 
        {
            tmp.text = text;
            tmp.color = color;
            initialColor = color;
        }
    }
}