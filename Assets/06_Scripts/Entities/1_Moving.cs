using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;

    Vector3? desiredLocation;
    Transform movingTarget;

    void Start()
    {
        entity = GetComponent<Entity>();
    }

    public void MoveTo(Vector3 destination)
    {
        Vector3 newTarget = new Vector3(
            destination.x,
            transform.position.y,
            destination.z
        );
        transform.LookAt(newTarget);
        desiredLocation = newTarget;
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
        }

        SetMoving(false);
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

        stepTo(location);
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
        stepTo(movingTarget.position);
    }

    void stepTo(Vector3 location)
    {
        SetMoving(true);
        Vector3 direction = (location - transform.position).normalized;
        transform.position +=
            direction * movingData.MovementSpeed * Time.deltaTime;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);
    }
}
