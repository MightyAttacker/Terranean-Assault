using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

        public void map1pick()
    {
        // Start a new game (same as PlayGame in MainMenu)
        SceneManager.LoadSceneAsync(2);
    }
}
