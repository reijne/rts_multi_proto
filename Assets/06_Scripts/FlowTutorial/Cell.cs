using UnityEngine;

public class TutorialCell
{
    public Vector3 worldPos;
    public Vector2Int gridIndex;
    public byte cost;
    public ushort bestCost;
    public TutorialGridDirection bestDirection;

    public TutorialCell(Vector3 _worldPos, Vector2Int _gridIndex)
    {
        worldPos = _worldPos;
        gridIndex = _gridIndex;
        cost = 1;
        bestCost = ushort.MaxValue;
        bestDirection = TutorialGridDirection.None;
    }

    public void IncreaseCost(int amnt)
    {
        if (cost == byte.MaxValue)
        {
            return;
        }
        if (amnt + cost >= 255)
        {
            cost = byte.MaxValue;
        }
        else
        {
            cost += (byte)amnt;
        }
    }
}
