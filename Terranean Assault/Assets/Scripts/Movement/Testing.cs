using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour
{
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;
    [SerializeField] private Hotbar hotbar; // Reference Hotbar
    [SerializeField] private Tilemap wallTilemap;

    private CharacterPathfindingMovementHandler selectedCharacter;
    private Pathfinding pathfinding;
    private List<CharacterPathfindingMovementHandler> characters = new List<CharacterPathfindingMovementHandler>();
    int[] attackerMovementPhases = { 2, 6, 10, 14, 18 };
    int[] defenderMovementPhases = { 4, 8, 12, 16, 20 };
    
    private void Start()
    {
        StartCoroutine(InitAfterTilemap());
    }

    private System.Collections.IEnumerator InitAfterTilemap()
    {
        yield return null; // Wait for tilemap initialization

        pathfinding = new Pathfinding(44, 30);

        pathfindingDebugStepVisual.Setup(pathfinding.GetGrid());
        pathfindingVisual.SetGrid(pathfinding.GetGrid());

        UpdateCharacterList();

        selectedCharacter = null;

        MarkWallsUnwalkable();
    }

    private void Update()
    {
        // Refresh character list dynamically
        UpdateCharacterList();

        if (hotbar.phase == 0 || hotbar.phase == 1)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = UtilsClass.GetMouseWorldPosition();
            TrySelectCharacter(mouseWorld);

            if (selectedCharacter != null && !IsClickOnCharacter(mouseWorld))
                TryMoveSelectedCharacter(mouseWorld);
        }
    }

    private void UpdateCharacterList()
    {
        characters.Clear();
        foreach (var unit in hotbar.spawnedUnits)
        {
            var handler = unit.GetComponent<CharacterPathfindingMovementHandler>();
            if (handler != null)
                characters.Add(handler);
        }
    }

    private void MarkWallsUnwalkable()
    {
        Grid<PathNode> grid = pathfinding.GetGrid();
        BoundsInt bounds = wallTilemap.cellBounds;

        foreach (Vector3Int cellPos in bounds.allPositionsWithin)
        {
            if (!wallTilemap.HasTile(cellPos)) continue;

            Vector3 worldPos = wallTilemap.CellToWorld(cellPos) + wallTilemap.cellSize / 2;
            grid.GetXY(worldPos, out int x, out int y);

            if (x >= 0 && y >= 0 && x < grid.GetWidth() && y < grid.GetHeight())
                grid.GetGridObject(x, y).SetIsWalkable(false);
        }

        Debug.Log("Marked wall tiles as unwalkable.");
    }

    private void TrySelectCharacter(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            var clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            if (clickedCharacter != null && characters.Contains(clickedCharacter))
            {
                // Enforce tag check
                if ((System.Array.Exists(attackerMovementPhases, p => p == hotbar.phase) && clickedCharacter.CompareTag(hotbar.attackerTag)) ||
        (System.Array.Exists(defenderMovementPhases, p => p == hotbar.phase) && clickedCharacter.CompareTag(hotbar.defenderTag)))
                {
                    selectedCharacter = clickedCharacter;
                    HighlightMovementRange(selectedCharacter);
                    Debug.Log($"Selected character: {selectedCharacter.name}");
                }
                else
                {
                    Debug.Log("Cannot select this unit in the current phase.");
                }
            }
        }
    }


    private bool IsClickOnCharacter(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
        var clickedChar = hit.collider?.GetComponent<CharacterPathfindingMovementHandler>();
        return clickedChar != null && characters.Contains(clickedChar);
    }

    private void TryMoveSelectedCharacter(Vector3 mouseWorldPosition)
    {
        if (selectedCharacter == null) return;

        pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);
        if (!IsWithinGridBounds(x, y)) return;

        Vector3 charWorldPos = selectedCharacter.transform.position;
        pathfinding.GetGrid().GetXY(charWorldPos, out int startX, out int startY);

        List<PathNode> path = pathfinding.FindPath(startX, startY, x, y);
        if (path == null) return;

        int totalCost = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            PathNode from = path[i];
            PathNode to = path[i + 1];
            totalCost += (from.x == to.x || from.y == to.y) ? 10 : 14;
        }

        int maxMoveCost = Mathf.FloorToInt(selectedCharacter.GetMaxMoveDistance() / pathfinding.GetGrid().GetCellSize()) * 10;

        if (totalCost > maxMoveCost)
        {
            Debug.Log("Destination too far based on movement cost");
            return;
        }

        pathfindingVisual.ClearHighlights();

        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;
        Vector3 targetCenter = new Vector3(x, y) * cellSize + cellOffset;

        selectedCharacter.SetTargetPosition(targetCenter);
        selectedCharacter = null;
    }

    private void HighlightMovementRange(CharacterPathfindingMovementHandler character)
    {
        if (character == null) return;

        pathfindingVisual.ClearHighlights();

        Vector3 charWorldPos = character.transform.position;
        pathfinding.GetGrid().GetXY(charWorldPos, out int charX, out int charY);

        int maxMoveCost = Mathf.FloorToInt(character.GetMaxMoveDistance() / pathfinding.GetGrid().GetCellSize()) * 10;
        List<PathNode> reachableNodes = GetReachableNodes(charX, charY, maxMoveCost);

        pathfindingVisual.HighlightNodes(reachableNodes, Color.blue);
    }

    private List<PathNode> GetReachableNodes(int startX, int startY, int maxMoveCost)
    {
        List<PathNode> reachableNodes = new();
        Grid<PathNode> grid = pathfinding.GetGrid();

        int width = grid.GetWidth();
        int height = grid.GetHeight();

        bool[,] visited = new bool[width, height];
        int[,] costSoFar = new int[width, height];

        Queue<PathNode> queue = new();

        PathNode startNode = grid.GetGridObject(startX, startY);
        visited[startX, startY] = true;
        costSoFar[startX, startY] = 0;

        queue.Enqueue(startNode);
        reachableNodes.Add(startNode);

        while (queue.Count > 0)
        {
            PathNode current = queue.Dequeue();

            foreach (PathNode neighbor in pathfinding.GetNeighbourList(current))
            {
                if (!neighbor.isWalkable) continue;

                int movementCost = (neighbor.x == current.x || neighbor.y == current.y) ? 10 : 14;
                int newCost = costSoFar[current.x, current.y] + movementCost;

                if (newCost <= maxMoveCost)
                {
                    if (!visited[neighbor.x, neighbor.y])
                    {
                        visited[neighbor.x, neighbor.y] = true;
                        costSoFar[neighbor.x, neighbor.y] = newCost;
                        queue.Enqueue(neighbor);
                        reachableNodes.Add(neighbor);
                    }
                    else if (newCost < costSoFar[neighbor.x, neighbor.y])
                    {
                        costSoFar[neighbor.x, neighbor.y] = newCost;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return reachableNodes;
    }

    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < pathfinding.GetGrid().GetWidth() && y < pathfinding.GetGrid().GetHeight();
    }
}
