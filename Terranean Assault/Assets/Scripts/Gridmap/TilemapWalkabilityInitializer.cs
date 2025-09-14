using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapWalkabilityInitializer : MonoBehaviour
{
    public Tilemap wallTilemap; // Assign the WallTilemap in the Inspector

    private void Start()
    {
        if (Pathfinding.Instance == null)
        {
            Debug.LogError("Pathfinding.Instance is null. Make sure it's initialized before this runs.");
            return;
        }

        Grid<PathNode> grid = Pathfinding.Instance.GetGrid();

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                Vector3 worldPos = grid.GetWorldPosition(x, y) + Vector3.one * grid.GetCellSize() * 0.5f;
                Vector3Int tilePos = wallTilemap.WorldToCell(worldPos);

                bool isWall = wallTilemap.HasTile(tilePos); // ⬅️ true if there's a wall tile

                PathNode node = grid.GetGridObject(x, y);
                if (node != null)
                {
                    node.SetIsWalkable(!isWall); // ⬅️ Not walkable if wall tile exists
                }
            }
        }

        Debug.Log("Walkability initialized based on WallTilemap.");
    }
}
