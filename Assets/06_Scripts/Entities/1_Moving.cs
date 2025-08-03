using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;
    PopulatedFlowField popFlowField;
    Transform movingTarget;

    Vector3 position => transform.position;

    void Start()
    {
        entity = GetComponent<Entity>();
    }

    public void MoveWith(PopulatedFlowField field)
    {
        popFlowField = field;
    }

    // Set a target transform to move towards, essentially following that transform.
    public void MoveTo(Transform target)
    {
        movingTarget = target;
    }

    void Update()
    {
        if (!entity.Enabled)
            return;

        move();
    }

    void move()
    {
        if (movingTarget != null)
        {
            moveToTarget();
            return;
        }

        if (popFlowField != null)
        {
            moveWithField();
            return;
        }
    }

    bool closeEnough(Vector3 location) =>
        Vector3.Distance(position, location) <= movingData.CloseEnoughDistance;

    void moveWithField()
    {
        Vector3 direction = popFlowField.GetDirection(transform.position);
        transform.position +=
            direction * movingData.MovementSpeed * Time.deltaTime;
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

        // If we can no longer move to the target, give up.
        if (!tryStepTo(movingTarget.position))
            movingTarget = null;
    }

    bool tryStepTo(Vector3 location)
    {
        Vector3 direction = (location - position).normalized;
        Vector3 newPosition =
            position + direction * movingData.MovementSpeed * Time.deltaTime;

        bool canMove =
            GridPlane.singleton.Equals(position, newPosition)
            || GridPlane.singleton.IsFree(newPosition);

        SetMoving(canMove);

        if (canMove)
            transform.position = newPosition;

        return canMove;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);

        if (moving) // We are leaving, free our position.
            GridPlane.singleton.Free(position);
        else // We have stopped moving, time to occupy.
            GridPlane.singleton.Occupy(position, CellType.Unit, entity);
    }
}
