using System.Collections.Generic;
using UnityEngine;

public class AttackRangeVisual : MonoBehaviour
{
    private Grid<PathNode> grid;
    [SerializeField] private Sprite highlightSprite;
    private readonly List<GameObject> highlightTiles = new();

    public void SetGrid(Grid<PathNode> grid)
    {
        this.grid = grid;
    }


    // Close-range attack visualization (melee)
    public void ShowCloseRangeAttack(Vector3 centerPos, int range)
    {
        ClearHighlights();
        grid.GetXY(centerPos, out int centerX, out int centerY);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                int checkX = centerX + x;
                int checkY = centerY + y;

                if (!grid.IsValidGridPosition(checkX, checkY)) continue;

                Vector3 tilePos = grid.GetWorldPosition(checkX, checkY) +
                                  new Vector3(grid.GetCellSize() * 0.5f, grid.GetCellSize() * 0.5f);
                CreateHighlightTile(tilePos, Color.red);
            }
        }
    }

    // Long-range attack visualization (like a bullet), blocked by obstacles
    public void ShowLongRangeAttack(Vector3 centerPos, int range)
    {
        ClearHighlights();
        grid.GetXY(centerPos, out int centerX, out int centerY);

        // Directions: 8-way (N, S, E, W, NE, NW, SE, SW)
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // right
            new Vector2Int(-1, 0),  // left
            new Vector2Int(0, 1),   // up
            new Vector2Int(0, -1),  // down
            new Vector2Int(1, 1),   // up-right
            new Vector2Int(-1, 1),  // up-left
            new Vector2Int(1, -1),  // down-right
            new Vector2Int(-1, -1)  // down-left
        };

        foreach (var dir in directions)
        {
            for (int i = 1; i <= range; i++)
            {
                int checkX = centerX + dir.x * i;
                int checkY = centerY + dir.y * i;

                if (!grid.IsValidGridPosition(checkX, checkY)) break;

                PathNode node = grid.GetGridObject(checkX, checkY);
                if (!node.isWalkable) break; // Stop at obstacles

                Vector3 tilePos = grid.GetWorldPosition(checkX, checkY) +
                                  new Vector3(grid.GetCellSize() * 0.5f, grid.GetCellSize() * 0.5f);
                CreateHighlightTile(tilePos, Color.yellow);
            }
        }
    }

    public void ClearHighlights()
    {
        foreach (var tile in highlightTiles)
            Destroy(tile);
        highlightTiles.Clear();
    }

    private void CreateHighlightTile(Vector3 position, Color color)
    {
        GameObject highlight = new GameObject("AttackRangeTile");
        highlight.transform.position = position;
        var sr = highlight.AddComponent<SpriteRenderer>();
        sr.sprite = highlightSprite;
        sr.color = new Color(color.r, color.g, color.b, 0.25f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 15;
        highlightTiles.Add(highlight);
    }
}
