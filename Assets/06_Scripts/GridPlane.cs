using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellType
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

    [SerializeField]
    private Grid grid;
    public Vector3 CellSize => grid.cellSize;
    public Vector2Int GridSize { get; private set; }

    private Material material;

    public FlowField flowField { get; private set; }
    public PopulatedFlowField populatedFlowField { get; private set; }

    // Entities tracked by world cell position
    private Dictionary<Vector3Int, Tuple<CellType, Entity>> cells =
        new Dictionary<Vector3Int, Tuple<CellType, Entity>>();

    private bool showFlowField = false;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        DontDestroyOnLoad(gameObject);

        int sizeX = Mathf.RoundToInt(
            transform.localScale.x * 10 / grid.cellSize.x
        );
        int sizeY = Mathf.RoundToInt(
            transform.localScale.z * 10 / grid.cellSize.z
        );

        // Center the grid on this plane.
        Vector3Int offset = new Vector3Int(sizeX / 2, 0, sizeY / 2);
        grid.gameObject.transform.position -= offset;
        gameObject.transform.position += offset;

        GridSize = new Vector2Int(sizeX, sizeY);

        material = GetComponent<MeshRenderer>().material;
        material.mainTextureScale = new Vector2(sizeX, sizeY);
    }

    void Start()
    {
        flowField = new FlowField(grid, GridSize);
    }

    public Vector3Int WorldToCell(Vector3 worldPos) =>
        grid.WorldToCell(worldPos);

    public Vector3 CellToWorld(Vector3Int cell) =>
        grid.GetCellCenterWorld(cell);

    private bool equals(Vector3 a, Vector3 b) =>
        WorldToCell(a) == WorldToCell(b);

    public bool Equals(Vector3 a, Vector3 b) => equals(a, b);

    // Functions to check the state of a cell.
    private bool isOccupied(Vector3Int cell) => cells.ContainsKey(cell);

    public bool IsOccupied(Vector3 world) => isOccupied(WorldToCell(world));

    private bool isFree(Vector3Int cell) => !isOccupied(cell);

    public bool IsFree(Vector3 world) => isFree(WorldToCell(world));

    // Functions to free or occupy a cell.
    private void free(Vector3Int cell) => cells.Remove(cell);

    public void Free(Vector3 world) => free(WorldToCell(world));

    private void occupy(Vector3Int cell, CellType type, Entity ent) =>
        cells[cell] = new Tuple<CellType, Entity>(type, ent);

    public void Occupy(Vector3 world, CellType c, Entity ent) =>
        occupy(WorldToCell(world), c, ent);

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
        Vector3Int? closest = getClosestAvailable(WorldToCell(desired));
        if (closest.HasValue)
            return CellToWorld(closest.Value);

        return null;
    }

    private Vector3 moveUnitToCell(Vector3Int cellPosition)
    {
        // cells.Add(cellPosition, Cell.Unit);
        return grid.GetCellCenterWorld(cellPosition);
    }

    public Vector3 MoveTo(Vector3 desired)
    {
        return CellToWorld(WorldToCell(desired));
    }

    public Vector3 MoveTo(Vector3 current, Vector3 desired)
    {
        Vector3Int desiredCell = grid.WorldToCell(desired);
        if (!cells.ContainsKey(desiredCell))
        {
            DebugHighlightCellBox(desiredCell, Color.yellow);
            return moveUnitToCell(desiredCell);
        }

        Vector3Int? closest = getClosestAvailable(WorldToCell(desired));
        if (closest.HasValue)
        {
            DebugHighlightCellBox(closest.Value, Color.blue);
            return moveUnitToCell(closest.Value);
        }

        DebugHighlightCellBox(grid.WorldToCell(current), Color.magenta);
        // TODO: Determine an actual proper fallback.
        return current;
    }

    private List<Tuple<CellType, Entity>> getCellsInRange(
        Vector3Int center,
        int visionRadius,
        CellType filter
    )
    {
        List<Tuple<CellType, Entity>> inRange =
            new List<Tuple<CellType, Entity>>();
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

    public List<Tuple<CellType, Entity>> GetCellsInRange(
        Vector3 position,
        int vision,
        CellType filter
    )
    {
        return getCellsInRange(WorldToCell(position), vision, filter);
    }

    void setDestination()
    {
        Game.singleton.GetHit()
            .ifJust(hit =>
            {
                DebugHighlightCellBox(WorldToCell(hit), Color.red);
                // TODO: Store the created flow field for later use in case
                // we want to move to the same location.
                populatedFlowField = flowField.Create(hit);
            });
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            setDestination();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            showFlowField = !showFlowField;
            // flowField?.DebugDrawGrid();
        }

        if (showFlowField)
            flowField?.DebugDrawGrid();

        // foreach (Vector3Int cellPos in cells.Keys)
        // {
        //     DebugHighlightCellBox(cellPos, Color.green);
        // }
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
