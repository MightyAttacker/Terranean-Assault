using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveMenu : MonoBehaviour
{
    public void Back()
    {
       
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        GameUtils.QuitGame();
    }
}