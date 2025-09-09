using UnityEngine;
public class Pathfinding
{
    private GridMapGrid<PathNode> grid;
    public Pathfinding(int width, int height)
    {
        grid = new GridMapGrid<PathNode>(
            width,
            height,
            1f,
            Vector3.zero,
            (GridMapGrid<PathNode> g, int x, int y) => new PathNode(g, x, y));
    }
}
