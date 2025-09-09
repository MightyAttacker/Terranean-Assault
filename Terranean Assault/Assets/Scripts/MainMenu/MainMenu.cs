using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }
    public void Options()
    {
        Debug.Log("Options button clicked (currently does nothing).");
    }
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        // If in Editor, stop playing the scene to simulate quitting
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
