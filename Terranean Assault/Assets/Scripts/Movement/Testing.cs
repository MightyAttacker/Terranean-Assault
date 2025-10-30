using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using CodeMonkey.Utils;

public class Testing : MonoBehaviour
{
    // Author - Lachlan Klenk
    [SerializeField] private PathfindingDebugStepVisual pathfindingDebugStepVisual;
    [SerializeField] private PathfindingVisual pathfindingVisual;
    [SerializeField] private Hotbar hotbar; // Reference Hotbar
    [SerializeField] private Tilemap wallTilemap;

    private CharacterPathfindingMovementHandler selectedCharacter;
    private Pathfinding pathfinding;
    private List<CharacterPathfindingMovementHandler> characters = new List<CharacterPathfindingMovementHandler>();

    public ErrorDisplay errorDisplay;
    private GameObject movementGhost;

    // Phase categories
    private enum PhaseType { None, AttackerMove, DefenderMove, AttackerFight, DefenderFight }

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

        if (Input.GetMouseButtonDown(1)) // Right-click undo
        {
            TryUndoMove(mouseWorld);
        }

        // Update movement ghost visual
        if (selectedCharacter != null && movementGhost != null)
        {
            mouseWorld.z = 0f;
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

            // Check if placement valid
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

            // Walls
            Vector3Int wallCell = wallTilemap.WorldToCell(ghostPos);
            if (wallTilemap.GetTile(wallCell) != null) invalid = true;

            // Ghost color update
            var renderers = movementGhost.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                Color c = invalid ? Color.red : Color.white;
                c.a = 0.7f;
                r.color = c;
            }
        }
    }

    // ------------------------
    // Phase & Selection Logic
    // ------------------------

    private PhaseType GetCurrentPhaseType()
    {
        int phase = hotbar.phase;

        if (phase == 2 || phase == 6 || phase == 10 || phase == 14 || phase == 18)
            return PhaseType.AttackerMove;
        if (phase == 4 || phase == 8 || phase == 12 || phase == 16 || phase == 20)
            return PhaseType.DefenderMove;
        if (phase == 3 || phase == 7 || phase == 11 || phase == 15 || phase == 19)
            return PhaseType.AttackerFight;
        if (phase == 5 || phase == 9 || phase == 13 || phase == 17 || phase == 21)
            return PhaseType.DefenderFight;

        return PhaseType.None;
    }

    private void TrySelectCharacter(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
        if (hit.collider == null) return;

        var clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
        if (clickedCharacter == null) return;

        PhaseType phaseType = GetCurrentPhaseType();
        bool canSelect = false;

        if (phaseType == PhaseType.AttackerMove || phaseType == PhaseType.AttackerFight)
            canSelect = clickedCharacter.CompareTag(hotbar.attackerTag);
        else if (phaseType == PhaseType.DefenderMove || phaseType == PhaseType.DefenderFight)
            canSelect = clickedCharacter.CompareTag(hotbar.defenderTag);

        if (!canSelect)
        {
            errorDisplay.ShowError("Cannot select this unit in the current phase.");
            return;
        }

        if (clickedCharacter.LastMovedPhase == hotbar.phase)
        {
            errorDisplay.ShowError($"{clickedCharacter.name} has already acted this phase.");
            return;
        }

        selectedCharacter = clickedCharacter;
        HighlightPhaseRange(selectedCharacter, phaseType);

        // Create ghost
        if (movementGhost != null)
        {
            Destroy(movementGhost);
            movementGhost = null;
        }

        movementGhost = Instantiate(selectedCharacter.gameObject);

        foreach (var collider in movementGhost.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        foreach (var script in movementGhost.GetComponents<MonoBehaviour>())
            script.enabled = false;

        foreach (var renderer in movementGhost.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = renderer.color;
            c.a = 0.5f;
            renderer.color = c;
        }

        Debug.Log($"Selected character: {selectedCharacter.name}");
    }

    private bool IsClickOnCharacter(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
        var clickedChar = hit.collider?.GetComponent<CharacterPathfindingMovementHandler>();
        return clickedChar != null && characters.Contains(clickedChar);
    }

    // ------------------------
    // Movement Logic
    // ------------------------

    private void TryMoveSelectedCharacter(Vector3 mouseWorldPosition)
    {
        if (selectedCharacter == null) return;

        PhaseType phaseType = GetCurrentPhaseType();
        if (phaseType != PhaseType.AttackerMove && phaseType != PhaseType.DefenderMove)
        {
            errorDisplay.ShowError("Cannot move during this phase!");
            return;
        }

        var grid = pathfinding.GetGrid();
        grid.GetXY(mouseWorldPosition, out int x, out int y);

        int footprintWidth = selectedCharacter.width;
        int footprintHeight = selectedCharacter.height;

        int baseX = x;
        int baseY = y;

        // Validate placement
        for (int dx = 0; dx < footprintWidth; dx++)
        {
            for (int dy = 0; dy < footprintHeight; dy++)
            {
                int checkX = baseX + dx;
                int checkY = baseY + dy;

                if (!IsWithinGridBounds(checkX, checkY))
                {
                    errorDisplay.ShowError("Cannot move unit here — out of bounds!");
                    return;
                }

                Vector3 cellWorldPos = new Vector3(checkX + 0.5f, checkY + 0.5f, 0f);
                Vector3Int wallCell = wallTilemap.WorldToCell(cellWorldPos);
                if (wallTilemap.GetTile(wallCell) != null)
                {
                    errorDisplay.ShowError("Cannot move unit here — wall blocking path!");
                    return;
                }

                Collider2D hit = Physics2D.OverlapCircle(cellWorldPos, 0.4f);
                if (hit != null && (hit.CompareTag(hotbar.attackerTag) || hit.CompareTag(hotbar.defenderTag)))
                {
                    errorDisplay.ShowError("Cannot move unit here — space occupied!");
                    return;
                }
            }
        }

        Vector3 charWorldPos = selectedCharacter.transform.position;
        grid.GetXY(charWorldPos, out int startX, out int startY);

        List<PathNode> path = pathfinding.FindPath(startX, startY, baseX, baseY);
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
            errorDisplay.ShowError("Destination too far!");
            return;
        }

        pathfindingVisual.ClearHighlights();

        Vector3 targetPosition = new Vector3(
            baseX + (footprintWidth * 0.5f),
            baseY + (footprintHeight * 0.5f),
            0f
        );

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

    private void TryUndoMove(Vector3 mouseWorldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            var clickedCharacter = hit.collider.GetComponent<CharacterPathfindingMovementHandler>();
            if (clickedCharacter != null && characters.Contains(clickedCharacter))
            {
                if (clickedCharacter.LastMovedPhase == hotbar.phase)
                {
                    clickedCharacter.ResetMovementPhase(true);
                    Debug.Log($"{clickedCharacter.name}'s move undone for phase {hotbar.phase}.");
                }
            }
        }
    }

    // ------------------------
    // Highlighting System
    // ------------------------

    private void HighlightPhaseRange(CharacterPathfindingMovementHandler character, PhaseType phaseType)
    {
        if (character == null) return;

        pathfindingVisual.ClearHighlights();
        Vector3 charWorldPos = character.transform.position;
        pathfinding.GetGrid().GetXY(charWorldPos, out int charX, out int charY);

        Color highlightColor;

        if (phaseType == PhaseType.AttackerMove || phaseType == PhaseType.DefenderMove)
        {
            highlightColor = Color.blue;
            int maxMoveCost = Mathf.FloorToInt(character.GetMaxMoveDistance() / pathfinding.GetGrid().GetCellSize()) * 10;
            List<PathNode> reachableNodes = GetReachableNodes(charX, charY, maxMoveCost);
            pathfindingVisual.HighlightNodes(reachableNodes, highlightColor);
        }
        else if (phaseType == PhaseType.AttackerFight || phaseType == PhaseType.DefenderFight)
        {
            highlightColor = Color.red;
            float attackRangeWorld = 1f;

            // Get the attack range from the PlayerAttack component (set during initialization)
            if (character.TryGetComponent<PlayerAttack>(out var attackComponent))
            {
                attackRangeWorld = attackComponent.attackRange;
            }

            // Convert world-space attack range to grid-space
            int attackRangeTiles = Mathf.RoundToInt(attackRangeWorld / pathfinding.GetGrid().GetCellSize());
            attackRangeTiles = Mathf.Max(1, attackRangeTiles); // never less than 1

            // Get attack nodes using the proper tile range
            List<PathNode> attackNodes = GetAttackRangeNodes(charX, charY, attackRangeTiles);

            pathfindingVisual.HighlightNodes(attackNodes, highlightColor);
        }
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
                }
            }
        }

        return reachableNodes;
    }

    private List<PathNode> GetAttackRangeNodes(int startX, int startY, int range)
    {
        List<PathNode> attackNodes = new();
        Grid<PathNode> grid = pathfinding.GetGrid();

        for (int x = startX - range; x <= startX + range; x++)
        {
            for (int y = startY - range; y <= startY + range; y++)
            {
                if (IsWithinGridBounds(x, y))
                {
                    PathNode node = grid.GetGridObject(x, y);
                    if (node.isWalkable && (x != startX || y != startY))
                        attackNodes.Add(node);
                }
            }
        }

        return attackNodes;
    }

    // ------------------------
    // Utility
    // ------------------------

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

    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < pathfinding.GetGrid().GetWidth() && y < pathfinding.GetGrid().GetHeight();
    }
}
