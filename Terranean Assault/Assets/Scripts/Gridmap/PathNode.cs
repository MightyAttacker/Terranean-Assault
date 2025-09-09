using UnityEngine;
public class PathNode
{
    private GridMapGrid<PathNode> grid;
    private int x;
    private int y;

    public int gCost;
    public int hCost;
    public int fCost;

    public PathNode cameFromNode;

    public PathNode(GridMapGrid<PathNode> grid, int x, int y)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
    }
    public override string ToString()
    {
        return x + "," + y;
    }
}
