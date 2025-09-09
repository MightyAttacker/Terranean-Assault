using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameUtils
{
    public static void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}