using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public enum CellType
{
    Unit,
    EnemyUnit,
    Building,
    EnemyBuilding,
    Terrain,
}

class GridEntities
{
    private List<Entity>[,] entities;
    private int width;
    private int depth; // Really its the depth of the grid.

    public GridEntities(Vector2Int size)
    {
        width = size.x;
        depth = size.y;
        entities = new List<Entity>[width, depth];
    }

    public List<Entity> Get(int x, int z)
    {
        return entities[x, z] ?? new List<Entity> { };
    }

    /// <summary> Get the raw value from the entities. </summary>
    /// <returns> NULL if it does not exist, otherwise the value. </returns>
    public List<Entity> UnsafeGet(int x, int z)
    {
        return entities[x, z];
    }

    /// <summary> Add entity to a position </summary>
    /// <returns> Whether we could add to the position, false if unoccupied position. </returns>
    public bool Add(int x, int z, Entity ent)
    {
        List<Entity> existing = entities[x, z];

        if (existing == null)
        {
            entities[x, z] = new List<Entity>() { ent };
            return true;
        }

        existing.Add(ent);
        return false;
    }

    public bool Remove(int x, int z, Entity ent)
    {
        List<Entity> existing = entities[x, z];

        if (existing == null) // TODO: Should this throw?
            return false;

        return existing.Remove(ent);
    }
}

public class GridPlane : MonoBehaviour
{
    public static GridPlane singleton { get; private set; }

    [SerializeField]
    private Grid grid;
    public Grid Grid => grid;
    public Vector3 cellSize => grid.cellSize;

    public Vector2Int GridSize { get; private set; }

    public FlowField flowField { get; private set; }
    public PopulatedFlowField populatedFlowField { get; private set; }

    // Entities tracked by world cell position (x,z)
    private GridEntities entities;

    private bool showFlowField = false;
    private bool showOccupancy = false;

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        DontDestroyOnLoad(gameObject);

        setGridSize();
        entities = new GridEntities(GridSize);
        centerGridOnPlaneObject();
        scaleTextureToMatchGridSize();
    }

    void setGridSize()
    {
        int sizeX = Mathf.RoundToInt(
            transform.localScale.x * 10 / grid.cellSize.x
        );
        int sizeY = Mathf.RoundToInt(
            transform.localScale.z * 10 / grid.cellSize.z
        );
        GridSize = new Vector2Int(sizeX, sizeY);
    }

    void centerGridOnPlaneObject()
    {
        // Center the grid on this plane.
        Vector3Int offset = new Vector3Int(GridSize.x / 2, 0, GridSize.y / 2);
        grid.gameObject.transform.position -= offset;
        gameObject.transform.position += offset;
    }

    void scaleTextureToMatchGridSize()
    {
        GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(
            GridSize.x,
            GridSize.y
        );
    }

    void Start()
    {
        flowField = new FlowField(grid, GridSize);
    }

    public int GetCount(Vector3Int loc)
    {
        return entities.Get(loc.x, loc.z).Count;
    }

    public int FindIndex(Vector3Int loc, Entity ent)
    {
        return entities.Get(loc.x, loc.z).FindIndex(e => e == ent);
    }

    public void Spawn(Vector3Int loc, Entity ent)
    {
        entities.Add(loc.x, loc.z, ent);
    }

    /// <summary> Move an entity from a location, to the current one. </summary>
    /// <returns> Whether the entity actually moved grid positions. </returns>
    public bool Move(Vector3Int from, Vector3Int to, Entity ent)
    {
        // We did not move enough to cross into a new grid position.
        if (to.Equals(from))
            return false;

        entities.Remove(from.x, from.z, ent);
        entities.Add(to.x, to.z, ent);
        return true;
    }

    List<Entity> getEntitiesInRange(Vector3Int center, int range)
    {
        int minX = Mathf.Max(center.x - range, 0);
        int maxX = Mathf.Min(center.x + range, GridSize.x - 1);

        int minZ = Mathf.Max(center.z - range, 0);
        int maxZ = Mathf.Min(center.z + range, GridSize.y - 1);

        var result = new List<Entity>();
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
        {
            List<Entity> occupants = entities.Get(x, z);
            if (occupants.Count > 0)
                result.AddRange(occupants);
        }

        return result;
    }

    public ReadOnlyArray<Entity> GetEntitiesInRange(Vector3 position, int range)
    {
        return getEntitiesInRange(grid.WorldToCell(position), range).ToArray();
    }

    void setDestination()
    {
        Game.GetHit()
            .ifJust(hit =>
            {
                debugHighlightCellBox(grid.WorldToCell(hit), Color.red);
                // TODO: Store the created flow field for later use in case
                // we want to move to the same location?
                // Might not work when we have a different cost field hmmmm.
                populatedFlowField = flowField.Create(hit);
            });
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
            setDestination();

        if (Input.GetKeyDown(KeyCode.F))
            showFlowField = !showFlowField;

        if (Input.GetKeyDown(KeyCode.G))
            showOccupancy = !showOccupancy;
    }

    void FixedUpdate()
    {
        if (showFlowField)
            flowField?.DebugDrawDirections();

        if (showOccupancy)
            debugHighlightOccupancy();
    }

    void debugHighlightOccupancy()
    {
        for (int x = 0; x < GridSize.x; x++)
        for (int z = 0; z < GridSize.y; z++)
        {
            List<Entity> occupants = entities.Get(x, z);
            if (occupants.Count > 0)
                debugHighlightCellBox(new Vector3Int(x, 0, z), Color.green);
        }
    }

    // Draw the outline of a cell using Debug.DrawLine, only in Editor.
    void debugHighlightCellBox(Vector3Int gridPos, Color color)
    {
        Vector3 center = grid.GetCellCenterWorld(gridPos);
        Debug.Log($"highlight cell, worldPos: {center}");
        float halfWidth = grid.cellSize.x / 2f;
        float halfDepth = grid.cellSize.z / 2f;
        float y = 0.1f;

        Vector3 bl = center + new Vector3(-halfWidth, y, -halfDepth); // bottom-left
        Vector3 br = center + new Vector3(halfWidth, y, -halfDepth); // bottom-right
        Vector3 tr = center + new Vector3(halfWidth, y, halfDepth); // top-right
        Vector3 tl = center + new Vector3(-halfWidth, y, halfDepth); // top-left

        Debug.DrawLine(bl, br, color, Time.fixedDeltaTime);
        Debug.DrawLine(br, tr, color, Time.fixedDeltaTime);
        Debug.DrawLine(tr, tl, color, Time.fixedDeltaTime);
        Debug.DrawLine(tl, bl, color, Time.fixedDeltaTime);
    }
}
