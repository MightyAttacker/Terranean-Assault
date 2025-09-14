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

        // Start with no character selected
        selectedCharacter = null;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

            if (hit.collider != null)
            {
                // Clicked on something - check if it's a character
                CharacterPathfindingMovementHandler clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
                if (clickedCharacter != null && characters.Contains(clickedCharacter))
                {
                    // Select this character and highlight movement range
                    selectedCharacter = clickedCharacter;
                    HighlightMovementRange(selectedCharacter);
                    return; // Done processing click
                }
            }

            // If we got here, clicked somewhere else (not a character)
            // If a character is selected, try to move it to clicked spot
            if (selectedCharacter != null)
            {
                TryMoveSelectedCharacter();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Right click clears selection and highlights
            selectedCharacter = null;
            pathfindingVisual.ClearHighlights();

            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);

            if (IsWithinGridBounds(x, y))
            {
                var node = pathfinding.GetNode(x, y);
                node.SetIsWalkable(!node.isWalkable);
            }
        }
    }

    private void HandleMovementStarted()
    {
        // Movement started: clear highlights to hide movement range during movement
        pathfindingVisual.ClearHighlights();
    }

    private void HandleMovementStopped()
    {
        // Movement stopped: clear selection and highlights so user can pick next character
        selectedCharacter = null;
        pathfindingVisual.ClearHighlights();
    }

    private void TryMoveSelectedCharacter()
    {
        if (selectedCharacter == null) return;

        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
        pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);

        if (!IsWithinGridBounds(x, y)) return;

        Vector3 selectedCharacterWorldPos = selectedCharacter.transform.position;
        pathfinding.GetGrid().GetXY(selectedCharacterWorldPos, out int startX, out int startY);

        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;

        Vector3 characterCenter = new Vector3(startX, startY) * cellSize + cellOffset;
        Vector3 targetCenter = new Vector3(x, y) * cellSize + cellOffset;

        float distance = Vector3.Distance(characterCenter, targetCenter);
        float maxMoveDistance = selectedCharacter.GetMaxMoveDistance();

        if (distance > maxMoveDistance)
        {
            Debug.Log($"Target too far: {distance:F2} units. Max allowed: {maxMoveDistance}");
            return;
        }

        List<PathNode> path = pathfinding.FindPath(startX, startY, x, y);

        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 startPos = new Vector3(path[i].x, path[i].y) * cellSize + cellOffset;
                Vector3 endPos = new Vector3(path[i + 1].x, path[i + 1].y) * cellSize + cellOffset;
                Debug.DrawLine(startPos, endPos, Color.green, 1f);
            }

            Debug.DrawRay(targetCenter, Vector3.up, Color.red, 1f);
            selectedCharacter.SetTargetPosition(targetCenter);
        }
    }

    private void HighlightMovementRange(CharacterPathfindingMovementHandler character)
    {
        if (character == null) return;

        pathfindingVisual.ClearHighlights();

        Vector3 characterWorldPos = character.transform.position;
        pathfinding.GetGrid().GetXY(characterWorldPos, out int charX, out int charY);

        float maxMoveDistance = character.GetMaxMoveDistance();
        float cellSize = pathfinding.GetGrid().GetCellSize();

        int maxRangeInCells = Mathf.FloorToInt(maxMoveDistance / cellSize);

        List<PathNode> nodesInRange = new List<PathNode>();
        Grid<PathNode> grid = pathfinding.GetGrid();

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                float distX = Mathf.Abs(x - charX);
                float distY = Mathf.Abs(y - charY);
                float distance = Mathf.Sqrt(distX * distX + distY * distY);

                if (distance <= maxRangeInCells && grid.GetGridObject(x, y).isWalkable)
                {
                    nodesInRange.Add(grid.GetGridObject(x, y));
                }
            }
        }

        pathfindingVisual.HighlightNodes(nodesInRange, Color.blue);
    }

    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < pathfinding.GetGrid().GetWidth() && y < pathfinding.GetGrid().GetHeight();
    }
}
