using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public void Accept()
    {
        SceneManager.LoadSceneAsync(0);
    }
    public void Cancel()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
