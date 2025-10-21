using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveMenu : MonoBehaviour
{
    public void Back()
    {
       
        SceneManager.LoadSceneAsync(0);
    }

    public void QuitGame()
    {
        GameUtils.QuitGame();
    }
}