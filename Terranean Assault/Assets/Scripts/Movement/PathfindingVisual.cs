using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingVisual : MonoBehaviour
{
    private Grid<PathNode> grid;
    private Mesh mesh;
    private bool updateMesh;

    private List<GameObject> highlightTiles = new List<GameObject>(); // Only declare once here

    [SerializeField] private Sprite highlightSprite; 

    private void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void SetGrid(Grid<PathNode> grid)
    {
        this.grid = grid;
        UpdateVisual();

        grid.OnGridObjectChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<PathNode>.OnGridObjectChangedEventArgs e)
    {
        updateMesh = true;
    }

    private void LateUpdate()
    {
        if (updateMesh)
        {
            updateMesh = false;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        
        MeshUtils.CreateEmptyMeshArrays(grid.GetWidth() * grid.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                int index = x * grid.GetHeight() + y;
                Vector3 quadSize = new Vector3(1, 1) * grid.GetCellSize();

                PathNode pathNode = grid.GetGridObject(x, y);

                if (pathNode.isWalkable)
                {
                    quadSize = Vector3.zero;
                }

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, grid.GetWorldPosition(x, y) + quadSize * .5f, 0f, quadSize, Vector2.zero, Vector2.zero);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

    public void ClearVisual()
    {
        mesh.Clear();
    }

    // ✅ Highlight nodes in blue
    public void HighlightNodes(List<PathNode> nodes, Color color)
    {
        ClearHighlights();

        foreach (var node in nodes)
        {
            Vector3 pos = grid.GetWorldPosition(node.x, node.y) + new Vector3(grid.GetCellSize(), grid.GetCellSize()) * 0.5f;
            CreateHighlightTile(pos, color);
        }
    }

    private void CreateHighlightTile(Vector3 position, Color color)
    {
        GameObject highlight = new GameObject("HighlightTile");
        highlight.transform.position = position;
        var sr = highlight.AddComponent<SpriteRenderer>();
        sr.sprite = highlightSprite;
        sr.color = new Color(color.r, color.g, color.b, 0.5f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 100;

        highlightTiles.Add(highlight); // Add to list
    }

    public void ClearHighlights()
    {
        foreach (var tile in highlightTiles)
        {
            Destroy(tile);
        }
        highlightTiles.Clear();
    }
}
