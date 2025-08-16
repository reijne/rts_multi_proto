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

        stepTowards(stepDirection.Value);

        afterMove();
    }

    float getRandomCollision()
    {
        return Random.Range(
            -GridPlane.singleton.GridSize.x / 2f,
            GridPlane.singleton.GridSize.y / 2f
        );
    }

    void stepTowards(Vector3 desiredDirection)
    {
        Vector3 newPosition =
            position
            + desiredDirection * movingData.MovementSpeed * Time.fixedDeltaTime;

        Vector3Int newGridPosition = GridPlane.singleton.Grid.WorldToCell(
            position
        );
        int occupants = GridPlane.singleton.GetCount(newGridPosition);

        if (occupants == 0)
        {
            transform.LookAt(newPosition);
            transform.position = newPosition;
            Debug.Log($"No occupants, moving normally");
            return;
        }

        Vector3 randomOffset = Vector3.zero;
        for (int o = 0; o < occupants; o++)
        {
            randomOffset += new Vector3(
                getRandomCollision(),
                getRandomCollision(),
                getRandomCollision()
            );
        }

        Debug.Log($"Desired direction: {desiredDirection}");
        Debug.Log($"    random offset: {randomOffset.normalized}");

        Vector3 finalDirection = (
            desiredDirection
            + movingData.CollisionAvoidance * randomOffset.normalized
        ).normalized;

        transform.LookAt(finalDirection);
        transform.position +=
            finalDirection * movingData.MovementSpeed * Time.fixedDeltaTime;
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

    /// <summary> After moving, possibly update position in the grid. </summary>
    void afterMove()
    {
        Vector3Int newGridPosition = GridPlane.singleton.Grid.WorldToCell(
            position
        );
        GridPlane.singleton.Move(gridPosition, newGridPosition, entity);
        gridPosition = newGridPosition;
    }

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
