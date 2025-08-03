using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;
    PopulatedFlowField fieldToFormation;
    Vector3? formationPosition;
    Quaternion? formationRotation;

    Transform movingTarget;

    Vector3 position => transform.position;

    void Start()
    {
        entity = GetComponent<Entity>();
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

    // Set a target transform to move towards, essentially following that transform.
    public void MoveTo(Transform target)
    {
        movingTarget = target;
        SetMoving(true);
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
                direction * movingData.MovementSpeed * Time.deltaTime;
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
            direction * movingData.MovementSpeed * Time.deltaTime;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);
    }
}
