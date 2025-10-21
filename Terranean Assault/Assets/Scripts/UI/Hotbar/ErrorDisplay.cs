using UnityEngine;
using TMPro;
using System.Collections;

public class ErrorDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text messageText;
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float displayDuration = 2f; // seconds visible before fade
    public float fadeDuration = 1f;    // seconds to fade out

    public void ShowError(string message)
    {
        if (messageText == null || canvasGroup == null)
        {
            Debug.LogWarning("ErrorDisplay missing references!");
            return;
        }

        StopAllCoroutines();
        messageText.text = message;
        StartCoroutine(ShowThenFade());
    }

    private IEnumerator ShowThenFade()
    {
        canvasGroup.alpha = 1f;

        // Stay visible for displayDuration
        yield return new WaitForSeconds(displayDuration);

        // Fade out smoothly
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
