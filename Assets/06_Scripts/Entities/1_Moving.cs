using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;

    Vector3? desiredLocation;
    Transform movingTarget;
    float blockedTime = 0f;
    const float maxBlockedTime = 1.5f; // seconds

    // TODO: Add this back in if the position calculation becomes too
    // heavy to check each update for grid position.
    // Vector3Int gridPosition;

    void Start()
    {
        entity = GetComponent<Entity>();
        // gridPosition = GridPlane.singleton.WorldToCell(transform.position);
    }

    public void MoveTo(Vector3 desired)
    {
        // Vector3 newTarget = new Vector3(
        //     destination.x,
        //     transform.position.y,
        //     destination.z
        // );
        Vector3 destination = GridPlane.singleton.MoveTo(
            transform.position,
            desired
        );
        destination.y = 0f;
        transform.LookAt(destination);
        desiredLocation = destination;
        blockedTime = 0f;
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
        Vector3.Distance(transform.position, location)
        <= movingData.CloseEnoughDistance;

    void move(Vector3 location)
    {
        if (closeEnough(location))
        {
            SetMoving(false);
            desiredLocation = null;
            return;
        }

        if (!tryStepTo(location))
        {
            blockedTime += Time.deltaTime;
        }
        else
        {
            blockedTime = 0f;
        }

        // TODO: Set the maximum time to be blocked from moving into movingData?
        if (blockedTime >= maxBlockedTime)
            desiredLocation = null;
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
        tryStepTo(movingTarget.position);
    }

    bool tryStepTo(Vector3 location)
    {
        Vector3 direction = (location - transform.position).normalized;
        Vector3 newPosition =
            transform.position
            + direction * movingData.MovementSpeed * Time.deltaTime;

        bool hasMoved = GridPlane.singleton.TryMove(
            transform.position,
            newPosition
        );
        SetMoving(hasMoved);

        if (hasMoved)
            transform.position = newPosition;

        return hasMoved;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);
    }
}
