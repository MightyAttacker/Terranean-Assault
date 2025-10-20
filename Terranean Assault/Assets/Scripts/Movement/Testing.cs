using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour
{
    //Author - Lachlan Klenk
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;
    [SerializeField] private Hotbar hotbar; // Reference Hotbar
    [SerializeField] private Tilemap wallTilemap;

    private CharacterPathfindingMovementHandler selectedCharacter;
    private Pathfinding pathfinding;
    private List<CharacterPathfindingMovementHandler> characters = new List<CharacterPathfindingMovementHandler>();
    int[] attackerMovementPhases = { 2, 6, 10, 14, 18 };
    int[] defenderMovementPhases = { 4, 8, 12, 16, 20 };
    public ErrorDisplay errorDisplay;
    private GameObject movementGhost;

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

        Vector3 mouseWorld = UtilsClass.GetMouseWorldPosition();

        if (Input.GetMouseButtonDown(0)) // Left-click
        {
            TrySelectCharacter(mouseWorld);

            if (selectedCharacter != null && !IsClickOnCharacter(mouseWorld))
                TryMoveSelectedCharacter(mouseWorld);

        }

        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            TryUndoMove(mouseWorld);
        }

        if (selectedCharacter != null && movementGhost != null)
        {
            mouseWorld.z = 0f;

            // Snap to grid
            int footprintWidth = selectedCharacter.width;
            int footprintHeight = selectedCharacter.height;

            int baseX = Mathf.FloorToInt(mouseWorld.x);
            int baseY = Mathf.FloorToInt(mouseWorld.y);

            Vector3 ghostPos = new Vector3(
                baseX + footprintWidth * 0.5f,
                baseY + footprintHeight * 0.5f,
                0f
            );

            movementGhost.transform.position = ghostPos;

            // Overlapping units
            bool invalid = false;
            for (int dx = 0; dx < footprintWidth; dx++)
            {
                for (int dy = 0; dy < footprintHeight; dy++)
                {
                    Vector2 checkPos = new Vector2(baseX + dx + 0.5f, baseY + dy + 0.5f);
                    Collider2D hit = Physics2D.OverlapCircle(checkPos, 0.4f);
                    if (hit != null)
                    {
                        var handler = hit.GetComponent<CharacterPathfindingMovementHandler>();
                        if (handler != null && handler != selectedCharacter)
                        {
                            invalid = true;
                            break;
                        }
                    }
                }
                if (invalid) break;
            }


            // Walls (optional if you want)
            Vector3Int wallCell = wallTilemap.WorldToCell(ghostPos);
            if (wallTilemap.GetTile(wallCell) != null) invalid = true;

            // Change ghost color based on validity
            var renderers = movementGhost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                Color c = invalid ? Color.red : Color.white;
                c.a = 0.7f;
                r.color = c;
            }
        }

    }

    private void TryUndoMove(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            var clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            if (clickedCharacter != null && characters.Contains(clickedCharacter))
            {
                // Only undo if this unit moved in the current phase
                if (clickedCharacter.LastMovedPhase == hotbar.phase)
                {
                    clickedCharacter.ResetMovementPhase(true); // restore position
                    Debug.Log($"{clickedCharacter.name}'s move has been undone for phase {hotbar.phase}.");
                }
            }
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
                // FIRST: Check if unit has already moved this phase
                if (clickedCharacter.LastMovedPhase == hotbar.phase)
                {
                    Debug.Log($"{clickedCharacter.name} has already moved in phase {hotbar.phase}.");
                    return; // Don't allow selection or highlight
                }

                // THEN: Check if unit matches current phase tag
                bool canSelect =
                    (System.Array.Exists(attackerMovementPhases, p => p == hotbar.phase) && clickedCharacter.CompareTag(hotbar.attackerTag)) ||
                    (System.Array.Exists(defenderMovementPhases, p => p == hotbar.phase) && clickedCharacter.CompareTag(hotbar.defenderTag));

                if (canSelect)
                {
                    selectedCharacter = clickedCharacter;
                    HighlightMovementRange(selectedCharacter);
                    Debug.Log($"Selected character: {selectedCharacter.name}");
                    if (movementGhost == null)
                    {
                        movementGhost = Instantiate(selectedCharacter.gameObject);

                        // Disable colliders and scripts that affect logic
                        foreach (var collider in movementGhost.GetComponentsInChildren<Collider2D>())
                            collider.enabled = false;
                        foreach (var script in movementGhost.GetComponents<MonoBehaviour>())
                            script.enabled = false;

                        // Make it semi-transparent
                        var renderers = movementGhost.GetComponentsInChildren<SpriteRenderer>();
                        foreach (var r in renderers)
                        {
                            Color c = r.color;
                            c.a = 0.5f;
                            r.color = c;
                        }
                    }

                }
                else
                {
                    errorDisplay.ShowError("Cannot select this unit in the current phase.");
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

        var grid = pathfinding.GetGrid();
        grid.GetXY(mouseWorldPosition, out int x, out int y);
        if (!IsWithinGridBounds(x, y)) return;

        Vector3 charWorldPos = selectedCharacter.transform.position;
        grid.GetXY(charWorldPos, out int startX, out int startY);

        List<PathNode> path = pathfinding.FindPath(startX, startY, x, y);
        if (path == null) return;

        int totalCost = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            PathNode from = path[i];
            PathNode to = path[i + 1];
            totalCost += (from.x == to.x || from.y == to.y) ? 10 : 14;
        }

        int maxMoveCost = Mathf.FloorToInt(selectedCharacter.GetMaxMoveDistance() / grid.GetCellSize()) * 10;
        if (totalCost > maxMoveCost)
        {
            errorDisplay.ShowError("Destination too far based on movement cost");
            return;
        }

        pathfindingVisual.ClearHighlights();

        // --- Footprint info ---
        int footprintWidth = selectedCharacter.width;
        int footprintHeight = selectedCharacter.height;

        // Snap mouse click to bottom-left of footprint
        int baseX = x;
        int baseY = y;

        // Check if all cells the unit would occupy are free
        for (int dx = 0; dx < footprintWidth; dx++)
        {
            for (int dy = 0; dy < footprintHeight; dy++)
            {
                Vector3 cellPos = new Vector3(
                    baseX + dx + 0.5f,
                    baseY + dy + 0.5f,
                    0f
                );

                Collider2D hit = Physics2D.OverlapCircle(cellPos, 0.4f);
                if (hit != null && (hit.CompareTag(hotbar.attackerTag) || hit.CompareTag(hotbar.defenderTag)))
                {
                    errorDisplay.ShowError("Cannot move unit here — another unit is already occupying this space!");
                    return;
                }
            }
        }
        Debug.Log($"SelectedCharacter: {selectedCharacter.name}, Width: {selectedCharacter.width}, Height: {selectedCharacter.height}");

        // Calculate center position of the footprint
        Vector3 targetPosition = new Vector3(
        baseX + (footprintWidth * 0.5f),
        baseY + (footprintHeight * 0.5f)
    );

        // Move unit if valid
        if (selectedCharacter.TryMove(targetPosition, hotbar.phase))
        {
            selectedCharacter = null;
        }
        if (movementGhost != null)
        {
            Destroy(movementGhost);
            movementGhost = null;
        }
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
