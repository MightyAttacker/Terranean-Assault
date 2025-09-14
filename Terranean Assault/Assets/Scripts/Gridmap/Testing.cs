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

        if (characters.Count > 0)
        {
            selectedCharacter = characters[0];
            Debug.Log($"Initially selected character: {selectedCharacter.name}");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectCharacter();

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
                var node = pathfinding.GetNode(x, y);
                node.SetIsWalkable(!node.isWalkable);
            }
        }
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
                Debug.Log($"Selected character: {selectedCharacter.name}");
            }
        }
    }

    private void TryMoveSelectedCharacter()
    {
        Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
        pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);

        if (!IsWithinGridBounds(x, y)) return;

        // Get grid position of selected character
        Vector3 selectedCharacterWorldPos = selectedCharacter.transform.position;
        pathfinding.GetGrid().GetXY(selectedCharacterWorldPos, out int startX, out int startY);

        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;

        // Get centered world positions
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
            // Debug draw path
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

    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < pathfinding.GetGrid().GetWidth() && y < pathfinding.GetGrid().GetHeight();
    }
}
