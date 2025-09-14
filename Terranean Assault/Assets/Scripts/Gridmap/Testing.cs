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

        // Subscribe to movement events for all characters
        foreach (var character in characters)
        {
            character.OnMovementStarted += HandleMovementStarted;
            character.OnMovementStopped += HandleMovementStopped;
        }

        // Start with no selected character
        selectedCharacter = null;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();

            // First, try to select a character if clicked on one
            TrySelectCharacter(mouseWorldPosition);

            // If a character is selected and click is not on that character, try moving it
            if (selectedCharacter != null)
            {
                // If clicked on a character, selection already handled above, so only move if clicked elsewhere
                if (!IsClickOnCharacter(mouseWorldPosition))
                {
                    TryMoveSelectedCharacter(mouseWorldPosition);
                }
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
                // Optionally refresh visuals here
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
        // After moving, clear selection and highlights
        selectedCharacter = null;
        pathfindingVisual.ClearHighlights();
    }

    private void TrySelectCharacter(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            CharacterPathfindingMovementHandler clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
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
        if (hit.collider != null)
        {
            var clickedChar = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            return clickedChar != null && characters.Contains(clickedChar);
        }
        return false;
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

        // Calculate total path cost using pathfinding cost logic
        int totalCost = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            PathNode from = path[i];
            PathNode to = path[i + 1];
            totalCost += (from.x == to.x || from.y == to.y) ? 10 : 14; // straight or diagonal
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
        List<PathNode> reachableNodes = new List<PathNode>();
        Grid<PathNode> grid = pathfinding.GetGrid();

        bool[,] visited = new bool[grid.GetWidth(), grid.GetHeight()];
        int[,] costSoFar = new int[grid.GetWidth(), grid.GetHeight()];

        Queue<PathNode> queue = new Queue<PathNode>();

        PathNode startNode = grid.GetGridObject(startX, startY);
        visited[startX, startY] = true;
        costSoFar[startX, startY] = 0;

        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            PathNode current = queue.Dequeue();
            reachableNodes.Add(current);

            foreach (PathNode neighbor in pathfinding.GetNeighbourList(current))
            {
                if (!neighbor.isWalkable) continue;

                int movementCost = (neighbor.x == current.x || neighbor.y == current.y) ? 10 : 14; // straight or diagonal
                int newCost = costSoFar[current.x, current.y] + movementCost;

                if (newCost <= maxMoveCost && (!visited[neighbor.x, neighbor.y] || newCost < costSoFar[neighbor.x, neighbor.y]))
                {
                    visited[neighbor.x, neighbor.y] = true;
                    costSoFar[neighbor.x, neighbor.y] = newCost;
                    queue.Enqueue(neighbor);
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
