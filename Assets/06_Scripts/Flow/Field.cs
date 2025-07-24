using System.Collections.Generic;
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
        Right,
        Back,
        Left,
    };

    /// <summary>
    /// N, NE, E, SE, S, SW, W, NW </summary>
    public static readonly Vector3Int[] CardinalPlus = new Vector3Int[8]
    {
        Forward,
        ForwardRight,
        Right,
        BackRight,
        Back,
        BackLeft,
        Left,
        ForwardLeft,
    };
}

// A single occupiable space in the Grid, used for FlowField path finding.
class Cell
{
    public readonly Vector3Int GridPosition;

    // World position, center of the grid position.
    // Stored for efficiency, equivalent of grid.center(gridPosition).
    public readonly Vector3 WorldPosition;

    private readonly byte cost;

    // Cost of moving on/over this cell, used to define what is on this cell.
    public byte Cost;

    // Best cost to move to the desired destination, used to determine direction.
    public ushort BestCost;

    // Best direction to move to the desired destination from this cell.
    public Vector3 BestDirection;

    // Construct with a possible cost, to allow initialization with some occupancy.
    public Cell(Vector3Int gridPosition, Vector3 worldPosition, byte? cost)
    {
        GridPosition = gridPosition;
        WorldPosition = worldPosition;
        this.cost = cost ?? 1;
        Cost = this.cost;
        BestCost = ushort.MaxValue;
        BestDirection = Direction.Zero;
    }

    // Reset this Cell to be re-used in a new path finding calculation.
    // For this we need to ensure we do not have the previous.
    public void Reset()
    {
        Cost = cost; // Reset to initial cost on constructor.
        BestCost = ushort.MaxValue;
        BestDirection = Direction.Zero;
    }
}

public class FlowField
{
    private static ushort min(ushort a, ushort b) => a < b ? a : b;

    // The actual grid in the world, used for translating between world and
    // grid space.
    readonly Grid grid;

    // TODO: Determine if we actually want to store this.
    // // The desired location we wish to go to, used for drawing the flow field.
    // readonly Vector3Int DestinationCell;

    bool shouldResetBeforeCreate = false;

    // The entire collection of cells, based on the grid position.
    Dictionary<Vector3Int, Cell> gridCells = new Dictionary<Vector3Int, Cell>();

    // TODO: Add a constructor that allows initial occupancy of cells,
    // with costs per grid position.
    public FlowField(Grid grid, Vector2 size)
    {
        this.grid = grid;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector3Int gridPosition = new Vector3Int(x, 0, z);
                gridCells[gridPosition] = new Cell(
                    // TODO: Why do we need the gridPosition in the cell?
                    gridPosition,
                    grid.GetCellCenterWorld(gridPosition),
                    null
                );
            }
        }
    }

    void reset()
    {
        foreach (Cell cell in gridCells.Values)
        {
            cell.Reset();
        }
    }

    // Populate the cells with best costs, based on a destination.
    void createIntegrationField(Vector3Int destination)
    {
        Cell destinationCell = gridCells[destination];
        destinationCell.Cost = 0;
        destinationCell.BestCost = 0;

        Queue<Cell> cellsToCheck = new Queue<Cell>();
        cellsToCheck.Enqueue(destinationCell);
        while (cellsToCheck.Count > 0)
        {
            Cell current = cellsToCheck.Dequeue();
            for (int n = 0; n < 4; n++)
            {
                Cell neighbor;
                if (
                    !gridCells.TryGetValue(
                        current.GridPosition + Direction.Cardinals[n],
                        out neighbor
                    )
                    || neighbor.Cost == byte.MaxValue
                ) // This cell is not traversable.
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

    private void create(Vector3Int destination)
    {
        if (shouldResetBeforeCreate)
            reset();

        // Populate all the bestCosts, so we can determine bestDirection from those.
        createIntegrationField(destination);

        foreach (Cell cell in gridCells.Values)
        {
            ushort bestCost = cell.BestCost;

            for (int n = 0; n < 8; n++)
            {
                Vector3Int direction = Direction.CardinalPlus[n];
                Cell neighbor;
                if (
                    !gridCells.TryGetValue(
                        cell.GridPosition + direction,
                        out neighbor
                    )
                )
                {
                    continue;
                }

                // If the neighbor is closer to the destination, update our new
                // found best cost, and set the direction to it.
                if (neighbor.BestCost < bestCost)
                {
                    bestCost = neighbor.BestCost;
                    cell.BestDirection = direction;
                }
            }
        }

        shouldResetBeforeCreate = true;
    }

    public void Create(Vector3 destination)
    {
        Debug.Log("FlowField.Create, destination" + destination);
        create(grid.WorldToCell(destination));
        Debug.Log("FlowField.Create, done");
    }

    public Vector3 GetDirection(Vector3 current)
    {
        return gridCells[grid.WorldToCell(current)].BestDirection;
    }

    // Draw the grid, with color based on the best cost.
    public void DebugDrawGrid()
    {
        // Normalize the max color based on the max cost.
        ushort maxCost = 0;
        foreach (var cell in gridCells.Values)
            if (cell.BestCost != ushort.MaxValue)
                maxCost = (ushort)Mathf.Max(maxCost, cell.BestCost);

        foreach (Cell cell in gridCells.Values)
        {
            float normalized =
                (cell.BestCost == ushort.MaxValue)
                    ? 1f
                    : (cell.BestCost / (float)maxCost);

            // Map normalized [0,1] → HSV hue range [0,1] (0 = red, 1 = back to red)
            // Use full saturation and brightness for vivid rainbow.
            Color rainbow = Color.HSVToRGB(1f - normalized, 1f, 1f);

            Gizmos.color = rainbow;
            Gizmos.DrawCube(cell.WorldPosition, grid.cellSize);
        }
    }
}
