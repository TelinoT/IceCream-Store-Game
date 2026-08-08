using UnityEngine;
using System.Collections;

public class ShopSign : MonoBehaviour
{
    [Header("Settings")]
    public float flipDuration = 0.5f;

    private bool isFlipping = false;

    void OnMouseDown()
    {
        if (isFlipping || DayManager.Instance == null) return;

        // Morning: Flipped to Open
        if (DayManager.Instance.isWaitingToOpen)
        {
            StartCoroutine(HandleSignClick(true));
        }
        // Evening: Flipped to Closed
        else if (DayManager.Instance.isWaitingToClose)
        {
            StartCoroutine(HandleSignClick(false));
        }
    }

    private IEnumerator HandleSignClick(bool isOpening)
    {
        isFlipping = true;
        AudioManager.Instance.Play("ButtonPop"); 

        // 1. Flip by exactly 180 degrees relative to its current orientation
        yield return StartCoroutine(FlipRoutine());

        // 2. Tiny pause so the player can see the change
        yield return new WaitForSecondsRealtime(0.5f);

        // 3. Notify DayManager to continue the sequence
        if (isOpening)
        {
            DayManager.Instance.ConfirmShopOpened();
        }
        else
        {
            DayManager.Instance.ConfirmShopClosed();
        }
        
        isFlipping = false;
    }

    private IEnumerator FlipRoutine()
    {
        float elapsed = 0f;
        Quaternion startRot = transform.localRotation;
        
        // Multiply by a 180-degree local Y rotation to always spin halfway relative to where it currently is
        Quaternion targetRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        while (elapsed < flipDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flipDuration;
            float easeT = t * t * (3f - 2f * t);

            transform.localRotation = Quaternion.Lerp(startRot, targetRot, easeT);
            yield return null;
        }

        transform.localRotation = targetRot;

        //yield return new WaitForSeconds(1f);
    }
}