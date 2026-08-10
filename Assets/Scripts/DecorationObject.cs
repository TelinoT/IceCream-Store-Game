using UnityEngine;
using UnityEngine.UI;

public class DecorationObject : MonoBehaviour
{
    public int price;
    public DecorationCategory category; // For UI filtering

    public Sprite uiIcon;

    [HideInInspector] 
    public string DecorationID;

    void Awake()
    {
        if (transform.parent != null)
        {
            DecorationID = transform.parent.name + "_" + gameObject.name;
        }
        else
        {
            DecorationID = gameObject.name;
        }
    }
    
    void Start()
    {
        // Hide if not bought
        if (PlayerPrefs.GetInt(DecorationID + "_active", 0) == 0)
            gameObject.SetActive(false);
    }

    public void Activate()
    {
        // Deactivate all in same category so only one is active
        /*foreach (var deco in FindObjectsOfType<DecorationObject>())
        {
            if (deco.category == category)
                deco.gameObject.SetActive(false);
        }*/
        
        if (PlayerPrefs.GetInt(DecorationID + "_active", 0) == 1)
        {
            gameObject.SetActive(false);
            PlayerPrefs.SetInt(DecorationID + "_active", 0); // Save active choice
            PlayerPrefs.Save();
        }
        else
        {
            gameObject.SetActive(true);
            PlayerPrefs.SetInt(DecorationID + "_active", 1); // Save active choice
            PlayerPrefs.Save();
        }
    }

    public void Preview(bool state)
    {
        gameObject.SetActive(state);
    }

    public bool IsActive()
    {
        if (PlayerPrefs.GetInt(DecorationID + "_active", 0) == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Buy()
    {
        gameObject.SetActive(true);
        PlayerPrefs.SetInt(DecorationID + "_bought", 1);
        PlayerPrefs.SetInt(DecorationID + "_active", 1); // Save active choice
        PlayerPrefs.Save();
    }

    public bool IsBought()
    {
        return PlayerPrefs.GetInt(DecorationID + "_bought", 0) == 1;
    }
}
public enum DecorationCategory { WallLeft, Counter, WallRight, WallFront, Tables, Floor }