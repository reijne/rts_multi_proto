using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;

    public Entity entity { get; private set; }
    private Vector3 position => transform.position;

    PopulatedFlowField fieldToFormation;
    Vector3? formationPosition;
    Quaternion? formationRotation;

    MovingTarget? movingTarget;

    Vector3Int gridPosition;

    void Start()
    {
        entity = GetComponent<Entity>();

        gridPosition = GridPlane.singleton.Grid.WorldToCell(position);
        GridPlane.singleton.Spawn(gridPosition, entity);
    }

    public void MoveWith(PopulatedFlowField field)
    {
        fieldToFormation = field;
        SetMoving(true);
    }

    public void MoveWith(PopulatedFlowField field, Vector3 withFormation)
    {
        fieldToFormation = field;
        formationPosition = withFormation;
        transform.LookAt(field.destination);
        formationRotation = transform.rotation;
        SetMoving(true);
    }

    // Set a target transform to move towards, essentially following
    // that transform.
    public void MoveTo(Transform target, float closeEnoughDistance)
    {
        movingTarget = new MovingTarget(target, closeEnoughDistance);
        SetMoving(true);
    }

    void FixedUpdate()
    {
        if (!entity.IsEnabled)
            return;

        Vector3? stepDirection = getDesiredStep();
        if (!stepDirection.HasValue) // Nothing moved, will move, or wants to move.
            return;

        transform.position +=
            stepDirection.Value
            * movingData.MovementSpeed
            * Time.fixedDeltaTime;

        afterMove();
    }

    /// <summary> Move this entity according to its desired destination. </summary>
    Vector3? getDesiredStep()
    {
        if (movingTarget.HasValue)
            return moveToTarget(movingTarget.Value);

        if (fieldToFormation != null)
            return moveWithField();

        if (formationPosition.HasValue)
            return moveIntoFormation(formationPosition.Value);

        return null;
    }

    // How tightly to pack within a cell (0.5 = edges; 0.4 keeps margin).
    const float kInCellPack = 0.45f;

    // Offset inside a single grid cell based on index & count
    Vector3 IntraCellOffset(int count)
    {
        if (count <= 1)
            return Vector3.zero;

        // square-ish micro-formation
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);

        int index = Random.Range(0, count);

        int row = index / cols;
        int col = index % cols;

        Vector3 cellSize = GridPlane.singleton.cellSize;
        float stepX = cellSize.x * kInCellPack;
        float stepZ = cellSize.z * kInCellPack;

        // center the micro-grid in the cell
        float originX = -(cols - 1) * 0.5f * stepX;
        float originZ = -(rows - 1) * 0.5f * stepZ;

        return new Vector3(originX + col * stepX, 0f, originZ + row * stepZ);
    }

    void afterMove()
    {
        // where did we end up this step?
        Vector3Int newGridPos = GridPlane.singleton.Grid.WorldToCell(position);

        // update occupancy (early out if still same cell)
        if (!GridPlane.singleton.Move(gridPosition, newGridPos, entity))
            return;

        gridPosition = newGridPos;

        // micro-formation: offset within cell if multiple occupants
        int count = GridPlane.singleton.GetCount(gridPosition);
        if (count <= 1)
            return;

        int index = GridPlane.singleton.FindIndex(gridPosition, entity);
        if (index < 0)
            return; // shouldn't happen, but guard

        Vector3 center = GridPlane.singleton.Grid.GetCellCenterWorld(
            gridPosition
        );
        // Vector3 target = center + IntraCellOffset(index, count);

        // Snap (simplest). For smoother settle, replace with a lerp:
        // transform.position = Vector3.Lerp(transform.position, target, 0.5f);
        transform.position += IntraCellOffset(count);
    }

    // /// <summary> After moving, possibly update position in the grid. </summary>
    // void afterMove()
    // {
    //     Vector3Int gridPositionAfterStep = GridPlane.singleton.Grid.WorldToCell(
    //         position
    //     );
    //     int countBefore = GridPlane.singleton.GetCount(gridPositionAfterStep);

    //     if (
    //         !GridPlane.singleton.Move(
    //             gridPosition,
    //             gridPositionAfterStep,
    //             entity
    //         )
    //     )
    //         return;

    //     gridPosition = gridPositionAfterStep;
    //     int countAfter = GridPlane.singleton.GetCount(gridPosition);

    //     if (countAfter <= 1)
    //     {
    //         return;
    //     }

    //     transform.position +=
    // }

    /// <summary> Location is close enough according to movingData minimum. </summary>
    bool closeEnough(Vector3 location) =>
        Vector3.Distance(position, location) <= movingData.CloseEnoughDistance;

    /// <summary> Close enough, given a distance, with fallback for minimum default. </summary>
    bool closeEnough(Vector3 location, float closeEnoughDistance) =>
        Vector3.Distance(position, location) <= closeEnoughDistance
        || closeEnough(location);

    Vector3? moveIntoFormation(Vector3 formationPos)
    {
        if (closeEnough(formationPos))
        {
            SetMoving(false);
            formationPosition = null;
            if (formationRotation.HasValue)
            {
                transform.rotation = formationRotation.Value;
                formationRotation = null;
            }
            return null;
        }

        return getStep(formationPos);
    }

    Vector3? moveWithField()
    {
        Vector3 direction = fieldToFormation.GetDirection(transform.position);

        // We still have to walk along the field to a place that counts as destination.
        if (direction != Vector3.zero)
            return direction;

        fieldToFormation = null;

        if (!formationPosition.HasValue)
            SetMoving(false);

        return null;
    }

    Vector3? moveToTarget(MovingTarget movingTarget)
    {
        if (
            closeEnough(
                movingTarget.transform.position,
                movingTarget.closeEnoughDistance
            )
        )
        {
            SetMoving(false);
            this.movingTarget = null;
            return null;
        }

        // transform.LookAt(movingTarget.transform.position);
        return getStep(movingTarget.transform.position);
    }

    Vector3 getStep(Vector3 location)
    {
        Vector3 direction = (location - position).normalized;
        return direction;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);
    }
}

struct MovingTarget
{
    public readonly Transform transform;
    public readonly float closeEnoughDistance;

    public MovingTarget(Transform transform, float closeEnoughDistance)
    {
        this.transform = transform;
        this.closeEnoughDistance = closeEnoughDistance;
    }
}
