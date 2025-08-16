using System.Collections.Generic;
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

    void Awake()
    {
        entity = GetComponent<Entity>();
    }

    void Start()
    {
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

        Vector3 finalDirection = getFinalDirection(stepDirection.Value);

        transform.position +=
            finalDirection.normalized // <<< THIS BOI
            * movingData.MovementSpeed
            * Time.fixedDeltaTime;

        afterMove();
    }

    Vector3 getFinalDirection(Vector3 desiredDirection)
    {
        Vector3 desiredPosition =
            position
            + desiredDirection * movingData.MovementSpeed * Time.fixedDeltaTime;
        Vector3Int desiredGrid = GridPlane.singleton.Grid.WorldToCell(position);

        Vector3 combinedPush = Vector3.zero;

        List<Entity> occupants = GridPlane.singleton.entities.Get(
            desiredGrid.x,
            desiredGrid.z
        );

        foreach (Entity ent in occupants)
        {
            Vector3 push = desiredPosition - ent.transform.position;
            combinedPush += push * (1f / push.sqrMagnitude);
        }

        combinedPush.y = 0;

        Vector3 finalDirection =
            desiredDirection
            + movingData.CollisionAvoidance * combinedPush * occupants.Count;

        return finalDirection;
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
        Vector3Int gridPositionAfterStep = GridPlane.singleton.Grid.WorldToCell(
            position
        );

        if (
            GridPlane.singleton.Move(
                gridPosition,
                gridPositionAfterStep,
                entity
            )
        )
            gridPosition = gridPositionAfterStep;
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
