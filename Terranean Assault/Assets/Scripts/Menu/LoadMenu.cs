using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMenu : MonoBehaviour
{
    public void NewGame()
    {
        // Start a new game (same as PlayGame in MainMenu)
        SceneManager.LoadSceneAsync(9);
    }

    public void LoadGame()
    {
        // Placeholder for loading saved game data
        Debug.Log("Load Game button clicked (implement save/load system here).");
        // Example: Load a saved scene or game state
        // SceneManager.LoadSceneAsync("SavedScene");
        // You can integrate your save system here (e.g., PlayerPrefs, JSON, etc.)
    }
    public void Back()
    {
        // Start a new game (same as PlayGame in MainMenu)
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        GameUtils.QuitGame();
    }
}