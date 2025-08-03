using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;
    PopulatedFlowField fieldToPosition;

    // The possible offset from the popFlowField destination because we
    // want to move to our spot in the formation.
    Vector3? formationOffset;
    float distanceBeforeMove;

    // The original position this entity had before we started moving.
    Vector3? positionBeforeMove;

    Transform movingTarget;

    Vector3 position => transform.position;

    void Start()
    {
        entity = GetComponent<Entity>();
    }

    public void MoveWith(PopulatedFlowField field)
    {
        fieldToPosition = field;
        formationOffset = null;
        positionBeforeMove = transform.position;
        distanceBeforeMove = Vector3.Distance(
            transform.position,
            field.destination
        );
    }

    public void MoveWith(PopulatedFlowField field, Vector3 formationOff)
    {
        fieldToPosition = field;
        formationOffset = formationOff;
        positionBeforeMove = transform.position;
        distanceBeforeMove = Vector3.Distance(
            transform.position,
            field.destination
        );
    }

    // Set a target transform to move towards, essentially following that transform.
    public void MoveTo(Transform target)
    {
        movingTarget = target;
        positionBeforeMove = transform.position;
        distanceBeforeMove = Vector3.Distance(
            transform.position,
            target.position
        );
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

        if (fieldToPosition != null)
        {
            moveWithField();
            return;
        }
    }

    bool closeEnough(Vector3 location) =>
        Vector3.Distance(position, location) <= movingData.CloseEnoughDistance;

    Vector3 ComputeSeparation()
    {
        // Radius ~ 1 cell
        float r = Mathf.Max(
            GridPlane.singleton.CellSize.x,
            GridPlane.singleton.CellSize.z
        );
        var neighbors = GridPlane.singleton.GetCellsInRange(
            transform.position,
            2,
            CellType.Unit
        );
        Vector3 acc = Vector3.zero;
        foreach (var tup in neighbors)
        {
            var other = tup.Item2; // Entity
            if (!other || other.transform == transform)
                continue;

            Vector3 toMe = transform.position - other.transform.position;
            float dist = toMe.magnitude;
            if (dist <= 0.0001f || dist > r)
                continue;

            float push = Mathf.InverseLerp(r, 0f, dist); // stronger when closer
            acc += toMe.normalized * push;
        }

        // Tune this coefficient
        return acc * 0.75f;
    }

    void ApplyStep(Vector3 newPos, Vector3 facing)
    {
        transform.position = newPos;
        if (facing != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(facing),
                0.1f
            );
    }

    // Reuse a small buffer to avoid allocations:
    static readonly Collider[] _overlapBuf = new Collider[16];
    const float agentRadius = 0.35f;
    const float agentHeight = 1.7f;

    bool IsPassable(Vector3 candidate)
    {
        // Build a capsule roughly matching the unit's footprint and height.
        // Feet slightly above ground so we don't intersect with terrain thickness.
        Vector3 feet = candidate + Vector3.up * (agentRadius + 0.02f);
        Vector3 head = feet + Vector3.up * (agentHeight - agentRadius * 2f);

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            feet,
            head,
            agentRadius,
            _overlapBuf,
            6,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            var col = _overlapBuf[i];
            if (!col)
                continue;

            // IMPORTANT: ignore self (and any children)
            if (col.transform.IsChildOf(transform))
                continue;

            // If you put a Rigidbody on units, you can also do:
            // if (col.attachedRigidbody && col.attachedRigidbody.transform.IsChildOf(transform)) continue;

            return false; // something (not me) is in the way
        }

        return true;
    }

    void StepWithLocalAvoidance(Vector3 desiredDir, float step)
    {
        if (desiredDir == Vector3.zero)
            return;

        // Try forward
        Vector3 tryPos = transform.position + desiredDir * step;
        if (IsPassable(tryPos))
        {
            ApplyStep(tryPos, desiredDir);
            return;
        }

        // Try slight left/right sidestep
        Vector3 left = new Vector3(-desiredDir.z, 0f, desiredDir.x).normalized;
        Vector3 right = -left;

        Vector3 tryLeft =
            transform.position + (desiredDir + 0.6f * left).normalized * step;
        if (IsPassable(tryLeft))
        {
            ApplyStep(tryLeft, (tryLeft - transform.position).normalized);
            return;
        }

        Vector3 tryRight =
            transform.position + (desiredDir + 0.6f * right).normalized * step;
        if (IsPassable(tryRight))
        {
            ApplyStep(tryRight, (tryRight - transform.position).normalized);
            return;
        }

        Debug.Log($"StepWithLocalAvoidance last resort, not moving...");
        // As a last resort, don’t move this frame.
    }

    void moveWithField()
    {
        Vector3 fieldDirection = fieldToPosition.GetDirection(
            transform.position
        );
        Debug.Log($"moveWithField fieldDirection: {fieldDirection}");
        float scaledMoveSpeed = movingData.MovementSpeed * Time.deltaTime;

        if (!formationOffset.HasValue)
        {
            transform.position += fieldDirection * scaledMoveSpeed;
            return;
        }

        float distanceToDestination = Vector3.Distance(
            transform.position,
            fieldToPosition.destination
        );

        // 0 at start -> 1 at destination
        float weight = Mathf.InverseLerp(
            distanceBeforeMove,
            0f,
            distanceToDestination
        );

        Vector3 formationTargetPos =
            fieldToPosition.destination + formationOffset.Value;
        Vector3 formationDir = (
            formationTargetPos - transform.position
        ).normalized;

        // Add a light separation force (see next section)
        Vector3 separation = ComputeSeparation();

        Vector3 blended = (
            Vector3.Lerp(fieldDirection, formationDir, weight) + separation
        ).normalized;

        StepWithLocalAvoidance(blended, scaledMoveSpeed);
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
