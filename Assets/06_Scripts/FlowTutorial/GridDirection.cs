using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialGridDirection
{
    public readonly Vector2Int Vector;

    private TutorialGridDirection(int x, int y)
    {
        Vector = new Vector2Int(x, y);
    }

    public static implicit operator Vector2Int(TutorialGridDirection direction)
    {
        return direction.Vector;
    }

    public static TutorialGridDirection GetDirectionFromV2I(Vector2Int vector)
    {
        return CardinalAndIntercardinalDirections
            .DefaultIfEmpty(None)
            .FirstOrDefault(direction => direction == vector);
    }

    public static readonly TutorialGridDirection None =
        new TutorialGridDirection(0, 0);
    public static readonly TutorialGridDirection North =
        new TutorialGridDirection(0, 1);
    public static readonly TutorialGridDirection South =
        new TutorialGridDirection(0, -1);
    public static readonly TutorialGridDirection East =
        new TutorialGridDirection(1, 0);
    public static readonly TutorialGridDirection West =
        new TutorialGridDirection(-1, 0);
    public static readonly TutorialGridDirection NorthEast =
        new TutorialGridDirection(1, 1);
    public static readonly TutorialGridDirection NorthWest =
        new TutorialGridDirection(-1, 1);
    public static readonly TutorialGridDirection SouthEast =
        new TutorialGridDirection(1, -1);
    public static readonly TutorialGridDirection SouthWest =
        new TutorialGridDirection(-1, -1);

    public static readonly List<TutorialGridDirection> CardinalDirections =
        new List<TutorialGridDirection> { North, East, South, West };

    public static readonly List<TutorialGridDirection> CardinalAndIntercardinalDirections =
        new List<TutorialGridDirection>
        {
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest,
            West,
            NorthWest,
        };

    public static readonly List<TutorialGridDirection> AllDirections =
        new List<TutorialGridDirection>
        {
            None,
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest,
            West,
            NorthWest,
        };
}
