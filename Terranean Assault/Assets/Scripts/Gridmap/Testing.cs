using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using CodeMonkey;

public class Testing : MonoBehaviour {
    
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;
    [SerializeField] private CharacterPathfindingMovementHandler characterPathfinding;
    private Pathfinding pathfinding;

    private void Start() {
        pathfinding = new Pathfinding(44, 30);
        pathfindingDebugStepVisual.Setup(pathfinding.GetGrid());
        pathfindingVisual.SetGrid(pathfinding.GetGrid());
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
    Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
    pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);
    List<PathNode> path = pathfinding.FindPath(0, 0, x, y);
    if (path != null) {
        float cellSize = pathfinding.GetGrid().GetCellSize();
        Vector3 cellOffset = Vector3.one * cellSize * 0.5f;

        for (int i = 0; i < path.Count - 1; i++) {
            Vector3 startPos = new Vector3(path[i].x, path[i].y) * cellSize + cellOffset;
            Vector3 endPos = new Vector3(path[i + 1].x, path[i + 1].y) * cellSize + cellOffset;
            Debug.DrawLine(startPos, endPos, Color.green, 1f);
        }

        Vector3 targetPosition = new Vector3(x, y) * cellSize + cellOffset;
        Debug.Log($"Snapped target position: {targetPosition}");
        Debug.DrawRay(targetPosition, Vector3.up, Color.red, 1f);

        characterPathfinding.SetTargetPosition(targetPosition);
    }
}

        if (Input.GetMouseButtonDown(1))
            {
                Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                pathfinding.GetGrid().GetXY(mouseWorldPosition, out int x, out int y);
                pathfinding.GetNode(x, y).SetIsWalkable(!pathfinding.GetNode(x, y).isWalkable);
            }
    }

}