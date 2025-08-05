using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static class Direction
{
    /// <summary> Vector3Int(0, 0, 0) </summary>
    public static readonly Vector3Int Zero = Vector3Int.zero;

    // Cardinal directions.
    /// <summary> Vector3Int(0, 0, 1) </summary>
    public static readonly Vector3Int Forward = Vector3Int.forward;

    /// <summary> Vector3Int(1, 0, 0) </summary>
    public static readonly Vector3Int Right = Vector3Int.right;

    /// <summary> Vector3Int(0, 0, -1) </summary>
    public static readonly Vector3Int Back = Vector3Int.back;

    /// <summary> Vector3Int(-1, 0, 0) </summary>
    public static readonly Vector3Int Left = Vector3Int.left;

    // In between cardinal directions.
    /// <summary> Vector3Int(1, 0, 1) </summary>
    public static readonly Vector3Int ForwardRight = Forward + Right;

    /// <summary> Vector3Int(1, 0, -1) </summary>
    public static readonly Vector3Int BackRight = Back + Right;

    /// <summary> Vector3Int(-1, 0, -1) </summary>
    public static readonly Vector3Int BackLeft = Back + Left;

    /// <summary> Vector3Int(-1, 0, 1) </summary>
    public static readonly Vector3Int ForwardLeft = Forward + Left;

    // List of cardinal directions.
    /// <summary> N, E, S, W </summary>
    public static readonly Vector3Int[] Cardinals = new Vector3Int[4]
    {
        Forward,
        Back,
        Right,
        Left,
    };

    /// <summary>
    /// N, NE, E, SE, S, SW, W, NW </summary>
    public static readonly Vector3Int[] CardinalPlus = new Vector3Int[8]
    {
        Forward,
        Right,
        Back,
        Left,
        ForwardRight,
        BackRight,
        BackLeft,
        ForwardLeft,
    };
}

// A single occupiable space in the Grid, used for FlowField path finding.
class Cell
{
    // World position, center of the grid position.
    // Stored for efficiency, equivalent of grid.center(Vector3Int(x, 0, z)).
    public readonly Vector3 WorldPosition;

    public readonly int x;
    public readonly int z;

    private readonly byte cost;

    // Cost of moving on/over this cell, used to define what is on this cell.
    public byte Cost;

    // Best cost to move to the desired destination, used to determine direction.
    public ushort BestCost;

    // DEBUG: Best direction to move to the desired destination,
    // only used for DebugDrawDirections.
    public Vector3 BestDirection;

    // Construct with a possible cost, to allow initialization with some occupancy.
    public Cell(int x, int z, Vector3 worldPosition, byte? cost)
    {
        this.x = x;
        this.z = z;
        WorldPosition = worldPosition;
        this.cost = cost ?? 1;
        Cost = this.cost;
        BestCost = ushort.MaxValue;
        BestDirection = Vector3.zero;
    }

    // Reset this Cell to be re-used in a new path finding calculation.
    // For this we need to ensure we do not have the previous.
    public void Reset()
    {
        Cost = cost; // Reset to initial cost on constructor.
        BestCost = ushort.MaxValue;
        BestDirection = Vector3.zero;
    }
}

// Minimal resulting field to be used in path finding.
public class PopulatedFlowField
{
    private Vector3[,] directions;
    private Func<Vector3, Vector3Int> worldToCell;

    public Vector3 destination { get; private set; }

    public PopulatedFlowField(
        Vector3[,] directions,
        Func<Vector3, Vector3Int> worldToCell,
        Vector3 destination
    )
    {
        this.directions = directions;
        this.worldToCell = worldToCell;
        this.destination = destination;
    }

    public Vector3 GetDirection(Vector3 worldPosition)
    {
        Vector3Int cellPos = worldToCell(worldPosition);
        return directions[cellPos.x, cellPos.z];
    }
}

public class FlowField
{
    static readonly Vector3Int[] EMPTY_V3INT = new Vector3Int[0];
    static readonly Vector3[] EMPTY_V3 = new Vector3[0];

    // The actual grid in the world, used for translating between world and
    // grid space.
    readonly Grid grid;

    // TODO: Determine if we actually want to store this.
    // // The desired location we wish to go to, used for drawing the flow field.
    // readonly Vector3Int DestinationCell;

    bool shouldResetBeforeCreate = false;

    // The entire collection of cells, based on the grid position.
    Cell[,] gridCells;
    int width;
    int height;

    Material debugMaterial;
    Mesh cubeMesh;

    // TODO: Add a constructor that allows initial occupancy of cells,
    // with costs per grid position.
    public FlowField(Grid grid, Vector2Int size)
    {
        this.grid = grid;
        width = size.x;
        height = size.y;

        gridCells = new Cell[size.x, size.y];
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                gridCells[x, z] = new Cell(
                    x,
                    z,
                    grid.GetCellCenterWorld(new Vector3Int(x, 0, z)),
                    null
                );
            }
        }
    }

    bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < width && z < height;

    void forEachCell(Action<Cell> action)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                action(gridCells[x, z]);
            }
        }
    }

    void reset()
    {
        for (int x = 0; x < width; x++)
        for (int z = 0; z < height; z++)
            gridCells[x, z].Reset();
    }

    // Populate the cells with best costs, based on a destination.
    void createIntegrationField(
        Vector3Int destination, // Main destination.
        IEnumerable<Vector3Int> destinations // Any other destinations that are acceptable.
    )
    {
        Queue<Cell> cellsToCheck = new Queue<Cell>();

        Cell destinationCell = gridCells[destination.x, destination.z];
        destinationCell.Cost = 0;
        destinationCell.BestCost = 0;
        cellsToCheck.Enqueue(destinationCell);

        foreach (Vector3Int dest in destinations)
        {
            Cell destCell = gridCells[dest.x, dest.z];
            destCell.Cost = 0;
            destCell.BestCost = 0;
            cellsToCheck.Enqueue(destCell);
        }

        while (cellsToCheck.Count > 0)
        {
            Cell current = cellsToCheck.Dequeue();
            for (int n = 0; n < 4; n++)
            {
                int nx = current.x + Direction.Cardinals[n].x;
                int nz = current.z + Direction.Cardinals[n].z;

                if (!InBounds(nx, nz))
                    continue;

                Cell neighbor = gridCells[nx, nz];
                if (neighbor.Cost == byte.MaxValue) // This cell is not traversable.
                    continue;

                // Neighbors best cost is either the one that already exists, or
                // the current best cost, plus walking to said neighbor.
                ushort newCost = (ushort)(current.BestCost + neighbor.Cost);
                if (newCost < neighbor.BestCost)
                {
                    neighbor.BestCost = newCost;
                    cellsToCheck.Enqueue(neighbor);
                }
            }
        }
    }

    private PopulatedFlowField create(
        Vector3Int destination,
        IEnumerable<Vector3Int> destinations
    )
    {
        if (shouldResetBeforeCreate)
            reset();

        // Populate all the bestCosts, so we can determine bestDirection from those.
        createIntegrationField(destination, destinations);

        // Create a mapping to extract only the bestDirection from cells.
        Vector3[,] directionMap = new Vector3[width, height];
        for (int x = 0; x < width; x++)
        for (int z = 0; z < height; z++)
        {
            Cell current = gridCells[x, z];
            ushort bestCost = current.BestCost;
            Vector3 bestDirection = Vector3.zero;

            for (int n = 0; n < 8; n++)
            {
                Vector3Int direction = Direction.CardinalPlus[n];
                int nx = current.x + direction.x;
                int nz = current.z + direction.z;

                if (!InBounds(nx, nz))
                    continue;

                Cell neighbor = gridCells[nx, nz];

                // If the neighbor is closer to the destination, update our new
                // found best cost, and set the direction to it.
                if (neighbor.BestCost < bestCost)
                {
                    bestCost = neighbor.BestCost;
                    bestDirection = direction;
                }
            }

            current.BestDirection = bestDirection.normalized;
            directionMap[x, z] = bestDirection.normalized;
        }

        shouldResetBeforeCreate = true;

        return new PopulatedFlowField(
            directionMap,
            worldPos => grid.WorldToCell(worldPos),
            grid.GetCellCenterWorld(destination)
        );
    }

    public PopulatedFlowField Create(Vector3 destination)
    {
        return create(grid.WorldToCell(destination), EMPTY_V3INT);
    }

    public PopulatedFlowField Create(
        Vector3 destination,
        IEnumerable<Vector3> destinations
    )
    {
        Vector3Int[] gridDestinations = new Vector3Int[destinations.Count()];
        int i = 0;
        foreach (Vector3 dest in destinations)
        {
            gridDestinations[i++] = grid.WorldToCell(dest);
        }
        return create(grid.WorldToCell(destination), gridDestinations);
    }

    public void DebugDrawDirections(
        float lineLength = 0.5f,
        Color? colorOverride = null
    )
    {
        Color color = colorOverride ?? Color.cyan;

        for (int x = 0; x < width; x++)
        for (int z = 0; z < height; z++)
        {
            Cell cell = gridCells[x, z];

            Debug.DrawLine(
                cell.WorldPosition,
                cell.WorldPosition
                    + cell.BestDirection.normalized
                        * (grid.cellSize.x + grid.cellSize.z)
                        * lineLength,
                color
            );
        }
    }

    void InitDebugResources()
    {
        if (debugMaterial == null)
        {
            debugMaterial = new Material(
                Shader.Find("Custom/UnlitInstanceColor")
            );
            debugMaterial.enableInstancing = true;
        }

        if (cubeMesh == null)
            cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }

    public void DebugDrawGrid()
    {
        InitDebugResources();

        // 1. Find max cost for normalization
        ushort maxCost = 0;
        forEachCell(cell =>
        {
            if (cell.BestCost != ushort.MaxValue)
                maxCost = (ushort)Mathf.Max(maxCost, cell.BestCost);
        });

        // 2. Prepare lists for batching
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        List<Vector4> colors = new List<Vector4>();

        forEachCell(cell =>
        {
            float normalized =
                (cell.BestCost == ushort.MaxValue)
                    ? 1f
                    : (cell.BestCost / (float)maxCost);

            Color rainbow = Color.HSVToRGB(1f - normalized, 1f, 1f);
            // Green at destination → Red at farthest
            Color gradient = Color.Lerp(Color.green, Color.red, normalized);

            matrices.Add(
                Matrix4x4.TRS(
                    cell.WorldPosition,
                    Quaternion.identity,
                    grid.cellSize * 0.95f
                )
            );
            colors.Add(gradient);

            // 3. Draw in batches of 1023
            if (matrices.Count == 1023)
            {
                DrawBatch(matrices, colors);
                matrices.Clear();
                colors.Clear();
            }
        });

        if (matrices.Count > 0)
            DrawBatch(matrices, colors);
    }

    void DrawBatch(List<Matrix4x4> matrices, List<Vector4> colors)
    {
        var props = new MaterialPropertyBlock();
        props.SetVectorArray("_Color", colors);
        Graphics.DrawMeshInstanced(
            cubeMesh,
            0,
            debugMaterial,
            matrices,
            props,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            0,
            null,
            UnityEngine.Rendering.LightProbeUsage.Off,
            null
        );
    }
}
