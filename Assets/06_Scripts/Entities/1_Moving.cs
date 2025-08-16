using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;

    public Entity entity { get; private set; }
    private Vector3 position => transform.position;

    PopulatedFlowField fieldToFormation;
    Vector3? formationPosition;
    Quaternion? formationRotation;

    Transform movingTarget;

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
    public void MoveTo(Transform target)
    {
        movingTarget = target;
        SetMoving(true);
    }

    void FixedUpdate()
    {
        if (!entity.Enabled)
            return;

        move();
        afterMove();
    }

    /// <summary> Move this entity according to its desired destination. </summary>
    void move()
    {
        if (movingTarget != null)
        {
            moveToTarget();
            return;
        }

        if (fieldToFormation != null)
        {
            moveWithField();
            return;
        }

        if (formationPosition.HasValue)
        {
            moveIntoFormation(formationPosition.Value);
            return;
        }
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

    bool closeEnough(Vector3 location) =>
        Vector3.Distance(position, location) <= movingData.CloseEnoughDistance;

    void moveIntoFormation(Vector3 formationPos)
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
            return;
        }

        step(formationPos);
    }

    void moveWithField()
    {
        Vector3 direction = fieldToFormation.GetDirection(transform.position);

        // We still have to walk along the field to a place that counts as destination.
        if (direction != Vector3.zero)
        {
            transform.LookAt(transform.position + direction);
            transform.position +=
                direction * movingData.MovementSpeed * Time.fixedDeltaTime;
            return;
        }

        fieldToFormation = null;

        if (!formationPosition.HasValue)
            SetMoving(false);
    }

    void moveToTarget()
    {
        if (closeEnough(movingTarget.position))
        {
            SetMoving(false);
            movingTarget = null;
            return;
        }

        transform.LookAt(movingTarget);
        step(movingTarget.position);
    }

    void step(Vector3 location)
    {
        Vector3 direction = (location - position).normalized;
        transform.position +=
            direction * movingData.MovementSpeed * Time.fixedDeltaTime;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);
    }
}
