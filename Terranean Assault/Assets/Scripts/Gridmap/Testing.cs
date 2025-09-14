using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour
{
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;
    [SerializeField] private List<CharacterPathfindingMovementHandler> characters;

    private CharacterPathfindingMovementHandler selectedCharacter;
    private Pathfinding pathfinding;

    private void Start()
    {
        pathfinding = new Pathfinding(44, 30);
        pathfindingDebugStepVisual.Setup(pathfinding.GetGrid());
        pathfindingVisual.SetGrid(pathfinding.GetGrid());

        foreach (var character in characters)
        {
            character.OnMovementStarted += HandleMovementStarted;
            character.OnMovementStopped += HandleMovementStopped;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (selectedCharacter == null)
            {
                TrySelectCharacter();
            }
            else
            {
                TryMoveSelectedCharacter();
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
        selectedCharacter = null; // Deselect after movement starts
    }

    private void HandleMovementStopped()
    {
        // Nothing selected after movement, wait for user to pick next character
    }

    private void TrySelectCharacter()
    {
        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            var clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            if (clickedCharacter != null && characters.Contains(clickedCharacter))
            {
                selectedCharacter = clickedCharacter;
                HighlightMovementRange(selectedCharacter);
            }
        }
    }

    private void TryMoveSelectedCharacter()
    {
        if (selectedCharacter == null) return;

        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
        pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);

        if (!IsWithinGridBounds(x, y)) return;

        pathfinding.GetGrid().GetXY(selectedCharacter.transform.position, out int startX, out int startY);

        // Get reachable nodes using BFS (non-diagonal)
        int maxRangeInCells = Mathf.FloorToInt(selectedCharacter.GetMaxMoveDistance() / pathfinding.GetGrid().GetCellSize());
        List<PathNode> reachableNodes = GetReachableNodes(startX, startY, maxRangeInCells);

        // Check if clicked node is in reachable nodes
        PathNode targetNode = pathfinding.GetNode(x, y);
        if (!reachableNodes.Contains(targetNode))
        {
            Debug.Log("Target not reachable within movement range.");
            return;
        }

        List<PathNode> path = pathfinding.FindPath(startX, startY, x, y);
        if (path == null)
        {
            Debug.Log("No path found.");
            return;
        }

        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;
        Vector3 targetWorldPos = new Vector3(x, y) * cellSize + cellOffset;

        selectedCharacter.SetTargetPosition(targetWorldPos);
        pathfindingVisual.ClearHighlights();
        selectedCharacter = null; // Deselect after issuing move
    }

    private void HighlightMovementRange(CharacterPathfindingMovementHandler character)
    {
        if (character == null) return;

        pathfindingVisual.ClearHighlights();

        pathfinding.GetGrid().GetXY(character.transform.position, out int charX, out int charY);

        int maxRangeInCells = Mathf.FloorToInt(character.GetMaxMoveDistance() / pathfinding.GetGrid().GetCellSize());

        List<PathNode> reachableNodes = GetReachableNodes(charX, charY, maxRangeInCells);

        pathfindingVisual.HighlightNodes(reachableNodes, Color.blue);
    }

    private List<PathNode> GetReachableNodes(int startX, int startY, int maxRangeInCells)
    {
        List<PathNode> reachableNodes = new List<PathNode>();
        Grid<PathNode> grid = pathfinding.GetGrid();

        bool[,] visited = new bool[grid.GetWidth(), grid.GetHeight()];
        int[,] distance = new int[grid.GetWidth(), grid.GetHeight()];
        Queue<PathNode> queue = new Queue<PathNode>();

        PathNode startNode = grid.GetGridObject(startX, startY);
        visited[startX, startY] = true;
        distance[startX, startY] = 0;

        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            PathNode current = queue.Dequeue();
            reachableNodes.Add(current);

            if (distance[current.x, current.y] >= maxRangeInCells)
                continue;

            // Use non-diagonal neighbors to avoid diagonal movement for range calculation
            List<PathNode> neighbors = GetNonDiagonalNeighbors(current);

            foreach (var neighbor in neighbors)
            {
                if (!visited[neighbor.x, neighbor.y] && neighbor.isWalkable)
                {
                    visited[neighbor.x, neighbor.y] = true;
                    distance[neighbor.x, neighbor.y] = distance[current.x, current.y] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return reachableNodes;
    }

    private List<PathNode> GetNonDiagonalNeighbors(PathNode node)
    {
        List<PathNode> neighbors = new List<PathNode>();
        Grid<PathNode> grid = pathfinding.GetGrid();

        if (node.x - 1 >= 0) neighbors.Add(grid.GetGridObject(node.x - 1, node.y)); // Left
        if (node.x + 1 < grid.GetWidth()) neighbors.Add(grid.GetGridObject(node.x + 1, node.y)); // Right
        if (node.y - 1 >= 0) neighbors.Add(grid.GetGridObject(node.x, node.y - 1)); // Down
        if (node.y + 1 < grid.GetHeight()) neighbors.Add(grid.GetGridObject(node.x, node.y + 1)); // Up

        return neighbors;
    }

    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < pathfinding.GetGrid().GetWidth() && y < pathfinding.GetGrid().GetHeight();
    }
}
