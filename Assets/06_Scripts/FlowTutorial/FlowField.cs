using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialFlowField
{
    public TutorialCell[,] grid { get; private set; }
    public Vector2Int gridSize { get; private set; }
    public float cellRadius { get; private set; }
    public TutorialCell destinationCell;

    private float cellDiameter;

    public TutorialFlowField(float _cellRadius, Vector2Int _gridSize)
    {
        cellRadius = _cellRadius;
        cellDiameter = cellRadius * 2f;
        gridSize = _gridSize;
    }

    public void CreateGrid()
    {
        grid = new TutorialCell[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 worldPos = new Vector3(
                    cellDiameter * x + cellRadius,
                    0,
                    cellDiameter * y + cellRadius
                );
                grid[x, y] = new TutorialCell(worldPos, new Vector2Int(x, y));
            }
        }
    }

    public void CreateCostField()
    {
        Vector3 cellHalfExtents = Vector3.one * cellRadius;
        int terrainMask = LayerMask.GetMask("Impassible", "RoughTerrain");
        foreach (TutorialCell curCell in grid)
        {
            Collider[] obstacles = Physics.OverlapBox(
                curCell.worldPos,
                cellHalfExtents,
                Quaternion.identity,
                terrainMask
            );
            bool hasIncreasedCost = false;
            foreach (Collider col in obstacles)
            {
                if (col.gameObject.layer == 8)
                {
                    curCell.IncreaseCost(255);
                    continue;
                }
                else if (!hasIncreasedCost && col.gameObject.layer == 9)
                {
                    curCell.IncreaseCost(3);
                    hasIncreasedCost = true;
                }
            }
        }
    }

    public void CreateIntegrationField(TutorialCell _destinationCell)
    {
        destinationCell = _destinationCell;

        destinationCell.cost = 0;
        destinationCell.bestCost = 0;

        Queue<TutorialCell> cellsToCheck = new Queue<TutorialCell>();

        cellsToCheck.Enqueue(destinationCell);

        while (cellsToCheck.Count > 0)
        {
            TutorialCell curCell = cellsToCheck.Dequeue();
            List<TutorialCell> curNeighbors = GetNeighborCells(
                curCell.gridIndex,
                TutorialGridDirection.CardinalDirections
            );
            foreach (TutorialCell curNeighbor in curNeighbors)
            {
                if (curNeighbor.cost == byte.MaxValue)
                {
                    continue;
                }
                if (curNeighbor.cost + curCell.bestCost < curNeighbor.bestCost)
                {
                    curNeighbor.bestCost = (ushort)(
                        curNeighbor.cost + curCell.bestCost
                    );
                    cellsToCheck.Enqueue(curNeighbor);
                }
            }
        }
    }

    public void CreateFlowField()
    {
        foreach (TutorialCell curCell in grid)
        {
            List<TutorialCell> curNeighbors = GetNeighborCells(
                curCell.gridIndex,
                TutorialGridDirection.AllDirections
            );

            int bestCost = curCell.bestCost;

            foreach (TutorialCell curNeighbor in curNeighbors)
            {
                if (curNeighbor.bestCost < bestCost)
                {
                    bestCost = curNeighbor.bestCost;
                    curCell.bestDirection =
                        TutorialGridDirection.GetDirectionFromV2I(
                            curNeighbor.gridIndex - curCell.gridIndex
                        );
                }
            }
        }
    }

    private List<TutorialCell> GetNeighborCells(
        Vector2Int nodeIndex,
        List<TutorialGridDirection> directions
    )
    {
        List<TutorialCell> neighborCells = new List<TutorialCell>();

        foreach (Vector2Int curDirection in directions)
        {
            TutorialCell newNeighbor = GetCellAtRelativePos(
                nodeIndex,
                curDirection
            );
            if (newNeighbor != null)
            {
                neighborCells.Add(newNeighbor);
            }
        }
        return neighborCells;
    }

    private TutorialCell GetCellAtRelativePos(
        Vector2Int orignPos,
        Vector2Int relativePos
    )
    {
        Vector2Int finalPos = orignPos + relativePos;

        if (
            finalPos.x < 0
            || finalPos.x >= gridSize.x
            || finalPos.y < 0
            || finalPos.y >= gridSize.y
        )
        {
            return null;
        }
        else
        {
            return grid[finalPos.x, finalPos.y];
        }
    }

    public TutorialCell GetCellFromWorldPos(Vector3 worldPos)
    {
        float percentX = worldPos.x / (gridSize.x * cellDiameter);
        float percentY = worldPos.z / (gridSize.y * cellDiameter);

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.Clamp(
            Mathf.FloorToInt((gridSize.x) * percentX),
            0,
            gridSize.x - 1
        );
        int y = Mathf.Clamp(
            Mathf.FloorToInt((gridSize.y) * percentY),
            0,
            gridSize.y - 1
        );
        return grid[x, y];
    }
}
