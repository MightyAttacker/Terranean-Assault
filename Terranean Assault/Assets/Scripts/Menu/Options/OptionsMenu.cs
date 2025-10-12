using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public void Accept()
    {
        SceneManager.LoadSceneAsync(1);
    }
    public void Cancel()
    {
        SceneManager.LoadSceneAsync(1);
    }
}
