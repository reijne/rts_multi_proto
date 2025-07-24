using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Cell
{
    Unit,
    EnemyUnit,
    Building,
    EnemyBuilding,
    Terrain,
}

public class GridPlane : MonoBehaviour
{
    public static GridPlane singleton { get; private set; }
    public Grid grid;
    private Material material;

    // Entities tracked by world cell position
    private Dictionary<Vector3Int, Tuple<Cell, Entity>> cells =
        new Dictionary<Vector3Int, Tuple<Cell, Entity>>();

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

    private void occupy(Vector3Int cell, Cell type, Entity ent) =>
        cells[cell] = new Tuple<Cell, Entity>(type, ent);

    public void Occupy(Vector3 world, Cell c, Entity ent) =>
        occupy(worldToCell(world), c, ent);

    private Vector3Int? getClosestAvailable(
        Vector3Int startCell,
        int maxRadius = 5
    )
    {
        if (isFree(startCell))
            return startCell;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    // Skip center (startCell)
                    if (x == 0 && z == 0)
                        continue;

                    Vector3Int check = new Vector3Int(
                        startCell.x + x,
                        startCell.y,
                        startCell.z + z
                    );

                    if (isFree(check))
                        return check;
                }
            }
        }

        return null; // Nothing found
    }

    public Vector3? GetClosestAvailable(Vector3 desired)
    {
        Vector3Int? closest = getClosestAvailable(worldToCell(desired));
        if (closest.HasValue)
            return cellToWorld(closest.Value);

        return null;
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

        Vector3Int? closest = getClosestAvailable(worldToCell(desired));
        if (closest.HasValue)
        {
            DebugHighlightCellBox(closest.Value, Color.blue);
            return moveUnitToCell(closest.Value);
        }

        DebugHighlightCellBox(grid.WorldToCell(current), Color.magenta);
        // TODO: Determine an actual proper fallback.
        return current;
    }

    private List<Tuple<Cell, Entity>> getCellsInRange(
        Vector3Int center,
        int visionRadius,
        Cell filter
    )
    {
        Debug.Log(
            "getCellsInRange current keys: "
                + string.Join(", ", cells.Keys.Select(k => k.ToString()))
        );
        Debug.Log("gridPlane position.y" + (int)transform.position.y);
        List<Tuple<Cell, Entity>> inRange = new List<Tuple<Cell, Entity>>();
        for (int x = -visionRadius; x <= visionRadius; x++)
        {
            for (int z = -visionRadius; z <= visionRadius; z++)
            {
                Vector3Int checkCell = new Vector3Int(
                    center.x + x,
                    -10,
                    center.z + z
                );
                if (isOccupied(checkCell) && cells[checkCell].Item1 == filter)
                {
                    inRange.Add(cells[checkCell]);
                }
            }
        }
        return inRange;
    }

    public List<Tuple<Cell, Entity>> GetCellsInRange(
        Vector3 position,
        int vision,
        Cell filter
    )
    {
        return getCellsInRange(worldToCell(position), vision, filter);
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
