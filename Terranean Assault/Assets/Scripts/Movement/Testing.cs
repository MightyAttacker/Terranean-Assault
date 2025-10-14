using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour
{
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;
    [SerializeField] private List<CharacterPathfindingMovementHandler> characters;
    [SerializeField] private Tilemap wallTilemap; // Assign this in Inspector

    private CharacterPathfindingMovementHandler selectedCharacter;
    private Pathfinding pathfinding;

    private void Start()
    {
        StartCoroutine(InitAfterTilemap());
    }

    private IEnumerator InitAfterTilemap()
    {
        // Wait a frame to allow Unity to fully initialize tilemaps
        yield return null;

        pathfinding = new Pathfinding(44, 30);

        Debug.Log($"pathfinding: {(pathfinding != null)} | debugVisual: {(pathfindingDebugStepVisual != null)} | pathfindingVisual: {(pathfindingVisual != null)} | grid: {(pathfinding?.GetGrid() != null)}");
        pathfindingDebugStepVisual.Setup(pathfinding.GetGrid());
        pathfindingVisual.SetGrid(pathfinding.GetGrid());

        foreach (var character in characters)
        {
            character.OnMovementStarted += HandleMovementStarted;
            character.OnMovementStopped += HandleMovementStopped;
        }

        selectedCharacter = null;

        MarkWallsUnwalkable(); // Update pathfinding walkability based on tilemap
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

            if (IsWithinGridBounds(x, y))
            {
                grid.GetGridObject(x, y).SetIsWalkable(false);
            }
        }

        Debug.Log("Marked wall tiles as unwalkable.");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            TrySelectCharacter(mouseWorldPosition);

            if (selectedCharacter != null && !IsClickOnCharacter(mouseWorldPosition))
            {
                TryMoveSelectedCharacter(mouseWorldPosition);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);

            if (IsWithinGridBounds(x, y))
            {
                var node = pathfinding.GetNode(x, y);
                node.SetIsWalkable(!node.isWalkable);
                pathfindingVisual.ClearHighlights();
                if (selectedCharacter != null)
                {
                    HighlightMovementRange(selectedCharacter);
                }
            }
        }
    }

    private void HandleMovementStarted()
    {
        pathfindingVisual.ClearHighlights();
    }

    private void HandleMovementStopped()
    {
        selectedCharacter = null;
        pathfindingVisual.ClearHighlights();
    }

    private void TrySelectCharacter(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            var clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            if (clickedCharacter != null && characters.Contains(clickedCharacter))
            {
                selectedCharacter = clickedCharacter;
                HighlightMovementRange(selectedCharacter);
                Debug.Log($"Selected character: {selectedCharacter.name}");
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

        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;
        Vector3 targetCenter = new Vector3(x, y) * cellSize + cellOffset;

        selectedCharacter.SetTargetPosition(targetCenter);
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
