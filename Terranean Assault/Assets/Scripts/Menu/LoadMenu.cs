using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMenu : MonoBehaviour
{
    public void NewGame()
    {
        SceneManager.LoadSceneAsync(3);
    }

    public void LoadGame()
    {
        SceneManager.LoadSceneAsync(10);
    }
    public void Back()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        GameUtils.QuitGame();
    }
}