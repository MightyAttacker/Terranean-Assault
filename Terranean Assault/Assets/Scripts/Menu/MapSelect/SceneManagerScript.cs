using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    public void MainMenu()
    {
        SceneManager.LoadSceneAsync(1);
    }
    public void Options()
    {
        SceneManager.LoadSceneAsync(2);
    }
    
    public void QuitGame()
    {
        GameUtils.QuitGame();
    }
}
