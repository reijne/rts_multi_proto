using System.Collections.Generic;
using UnityEngine;

public enum Cell
{
    Unit = 1,
    Building = 2,
    Terrain = 3,
}

public class GridPlane : MonoBehaviour
{
    public static GridPlane singleton { get; private set; }
    public Grid grid;
    private Material material;

    // Cells tracked by world cell position
    private Dictionary<Vector3Int, Cell> cells =
        new Dictionary<Vector3Int, Cell>();

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        DontDestroyOnLoad(gameObject);

        // material = GetComponent<MeshRenderer>().material;
        // float scaleX = transform.localScale.x * 10 / grid.cellSize.x;
        // float scaleY = transform.localScale.z * 10 / grid.cellSize.z;
        // material.mainTextureScale = new Vector2(scaleX, scaleY);
    }

    private Vector3Int worldToCell(Vector3 worldPos) =>
        grid.WorldToCell(worldPos);

    private Vector3 cellToWorld(Vector3Int cell) =>
        grid.GetCellCenterWorld(cell);

    private bool equals(Vector3 a, Vector3 b) =>
        worldToCell(a) == worldToCell(b);

    public bool Equals(Vector3 a, Vector3 b) => equals(a, b);

    // Functions to check the state of a cell.
    private bool isOccupied(Vector3Int cell) => cells.ContainsKey(cell);

    public bool IsOccupied(Vector3 world) => isOccupied(worldToCell(world));

    private bool isFree(Vector3Int cell) => !isOccupied(cell);

    public bool IsFree(Vector3 world) => isFree(worldToCell(world));

    // Functions to free or occupy a cell.
    private void free(Vector3Int cell) => cells.Remove(cell);

    public void Free(Vector3 world) => free(worldToCell(world));

    private void occupy(Vector3Int cell, Cell type) => cells[cell] = type;

    public void Occupy(Vector3 world, Cell c) => occupy(worldToCell(world), c);

    private Vector3Int? getClosestAvailable(Vector3 current, Vector3 desired)
    {
        Vector3Int targetCell = grid.WorldToCell(desired);
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        queue.Enqueue(targetCell);
        visited.Add(targetCell);

        // Directions: up, down, left, right, diagonals
        Vector3Int[] directions =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(-1, 0, -1),
        };

        int maxSearchRadius = 10; // Safety limit
        int steps = 0;

        while (queue.Count > 0 && steps < 1000)
        {
            Vector3Int cell = queue.Dequeue();

            if (isFree(cell))
                return cell;

            foreach (var dir in directions)
            {
                Vector3Int neighbor = cell + dir;
                if (
                    !visited.Contains(neighbor)
                    && Vector3Int.Distance(neighbor, targetCell)
                        <= maxSearchRadius
                )
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }

            steps++;
        }

        return null; // No free cell found
    }

    private Vector3 moveUnitToCell(Vector3Int cellPosition)
    {
        // cells.Add(cellPosition, Cell.Unit);
        return grid.GetCellCenterWorld(cellPosition);
    }

    public Vector3 MoveTo(Vector3 current, Vector3 desired)
    {
        Vector3Int desiredCell = grid.WorldToCell(desired);
        if (!cells.ContainsKey(desiredCell))
        {
            DebugHighlightCellBox(desiredCell, Color.yellow);
            return moveUnitToCell(desiredCell);
        }

        Vector3Int? closest = getClosestAvailable(current, desired);
        if (closest.HasValue)
        {
            DebugHighlightCellBox(closest.Value, Color.blue);
            return moveUnitToCell(closest.Value);
        }

        DebugHighlightCellBox(grid.WorldToCell(current), Color.magenta);
        // TODO: Determine an actual proper fallback.
        return current;
    }

    // DEBUG clicking.
    void OnMouseDown()
    {
        Game.singleton.GetHit()
            .ifJust(hit =>
            {
                Vector3Int cell = grid.WorldToCell(hit);
                DebugHighlightCellBox(cell, Color.white);
            });
    }

    void Update()
    {
        foreach (Vector3Int cellPos in cells.Keys)
        {
            DebugHighlightCellBox(cellPos, Color.green);
        }
    }

    // Draw the outline of a cell using Debug.DrawLine, only in Editor.
    public void DebugHighlightCellBox(Vector3Int cell, Color color)
    {
        Vector3 center = grid.GetCellCenterWorld(cell);
        float halfWidth = grid.cellSize.x / 2f;
        float halfDepth = grid.cellSize.z / 2f;
        float y = 0.1f;

        Vector3 bl = center + new Vector3(-halfWidth, y, -halfDepth); // bottom-left
        Vector3 br = center + new Vector3(halfWidth, y, -halfDepth); // bottom-right
        Vector3 tr = center + new Vector3(halfWidth, y, halfDepth); // top-right
        Vector3 tl = center + new Vector3(-halfWidth, y, halfDepth); // top-left

        Debug.DrawLine(bl, br, color, 3f);
        Debug.DrawLine(br, tr, color, 3f);
        Debug.DrawLine(tr, tl, color, 3f);
        Debug.DrawLine(tl, bl, color, 3f);
    }
}
