using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private Scrollbar brightnessScrollbar;
    [SerializeField] private Image screenOverlay;

    private void Start()
    {

        if (brightnessScrollbar != null && screenOverlay != null)
        {
            brightnessScrollbar.onValueChanged.AddListener(UpdateBrightness);

            brightnessScrollbar.value = PlayerPrefs.GetFloat("Brightness", 1f);
            UpdateBrightness(brightnessScrollbar.value);
        }
    }


    private void UpdateBrightness(float value)
    {
        if (screenOverlay != null)
        {

            float alpha = Mathf.Lerp(0.8f, 0f, value);
            Color overlayColor = screenOverlay.color;
            overlayColor.a = alpha;
            screenOverlay.color = overlayColor;
        }
    }

    public void Accept()
    {
        if (brightnessScrollbar != null)
            PlayerPrefs.SetFloat("Brightness", brightnessScrollbar.value);
        
        SceneManager.LoadSceneAsync(0);
    }

    public void Cancel()
    {

        SceneManager.LoadSceneAsync(0);
    }

    private void OnDestroy()
    {

        if (brightnessScrollbar != null)
            brightnessScrollbar.onValueChanged.RemoveListener(UpdateBrightness);
    }
}