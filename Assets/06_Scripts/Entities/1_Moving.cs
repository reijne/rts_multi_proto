using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;

    Vector3? desiredLocation;
    Transform movingTarget;

    Vector3 position => transform.position;

    // TODO: Add this back in if the position calculation becomes too
    // heavy to check each update for grid position.
    // Vector3Int gridPosition;

    void Start()
    {
        entity = GetComponent<Entity>();
        // gridPosition = GridPlane.singleton.WorldToCell(position);
    }

    public void MoveTo(Vector3 desired)
    {
        Vector3 destination = GridPlane.singleton.MoveTo(desired);
        destination.y = 0f;
        transform.LookAt(destination);
        desiredLocation = destination;
    }

    // Set a target transform to move towards, essentially following that transform.
    // Manual MoveTo a set location takes precedence over this target.
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
        if (desiredLocation.HasValue)
        {
            move(desiredLocation.Value);
            return;
        }

        if (movingTarget != null)
        {
            move(movingTarget);
            return;
        }
    }

    bool closeEnough(Vector3 location) =>
        Vector3.Distance(position, location) <= movingData.CloseEnoughDistance;

    void move(Vector3 location)
    {
        if (closeEnough(location))
        {
            SetMoving(false);
            desiredLocation = null;
            return;
        }

        // If we can no longer move towards the desired location, give up.
        if (tryStepTo(location))
            return;

        // TODO: determine if this bouncing around when stopping is better.
        desiredLocation = GridPlane.singleton.GetClosestAvailable(position);
    }

    void move(Transform target)
    {
        if (closeEnough(target.position))
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
