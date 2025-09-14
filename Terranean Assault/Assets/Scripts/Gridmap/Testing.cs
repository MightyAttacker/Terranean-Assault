using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour
{
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;

    // List of all characters you want to manage
    [SerializeField] private List<CharacterPathfindingMovementHandler> characters;

    private CharacterPathfindingMovementHandler selectedCharacter;
    private Pathfinding pathfinding;

    private void Start()
    {
        pathfinding = new Pathfinding(44, 30);
        pathfindingDebugStepVisual.Setup(pathfinding.GetGrid());
        pathfindingVisual.SetGrid(pathfinding.GetGrid());

        if (characters.Count > 0)
        {
            selectedCharacter = characters[0]; // Default select first character
        }
    }

    private void Update()
    {
        // Left click: first try to select a character
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectCharacter();

            // If a character is already selected, move it to clicked position on grid
            if (selectedCharacter != null)
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
                pathfinding.GetNode(x, y).SetIsWalkable(!pathfinding.GetNode(x, y).isWalkable);
            }
        }
    }

    private void TrySelectCharacter()
    {
        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
        // Raycast to see if you clicked a character
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            CharacterPathfindingMovementHandler clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            if (clickedCharacter != null && characters.Contains(clickedCharacter))
            {
                selectedCharacter = clickedCharacter;
                Debug.Log($"Selected character: {selectedCharacter.name}");
            }
        }
    }

    private void TryMoveSelectedCharacter()
{
    Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
    pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);

    if (!IsWithinGridBounds(x, y)) return;

    Vector3 selectedCharacterWorldPos = selectedCharacter.transform.position;
    pathfinding.GetGrid().GetXY(selectedCharacterWorldPos, out int startX, out int startY);

    List<PathNode> path = pathfinding.FindPath(startX, startY, x, y);

    if (path != null)
    {
        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 startPos = new Vector3(path[i].x, path[i].y) * cellSize + cellOffset;
            Vector3 endPos = new Vector3(path[i + 1].x, path[i + 1].y) * cellSize + cellOffset;
            Debug.DrawLine(startPos, endPos, Color.green, 1f);
        }

        Vector3 targetPosition = new Vector3(x, y) * cellSize + cellOffset;
        Debug.DrawRay(targetPosition, Vector3.up, Color.red, 1f);

        selectedCharacter.SetTargetPosition(targetPosition);
    }
}


    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < pathfinding.GetGrid().GetWidth() && y < pathfinding.GetGrid().GetHeight();
    }
}
