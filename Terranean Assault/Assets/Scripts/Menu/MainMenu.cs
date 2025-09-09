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
        GameUtils.QuitGame();
    }
}