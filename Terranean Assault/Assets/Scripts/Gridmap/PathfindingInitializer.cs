using UnityEngine;
// Put this on a GameObject early in the scene (like an empty "GameManager" object)
public class PathfindingInitializer : MonoBehaviour
{
    public int width = 44;
    public int height = 30;

    private void Awake()
    {
        if (Pathfinding.Instance == null)
        {
            new Pathfinding(width, height);
            Debug.Log("Pathfinding grid initialized.");
        }
        else
        {
            Debug.LogWarning("Pathfinding is already initialized.");
        }
    }
}
