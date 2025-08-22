using UnityEngine;
using UnityEngine.AI;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    public Entity entity { get; private set; }

    NavMeshAgent navMeshAgent;

    GameObject moveIndicator;
    MovingTarget? movingTarget;
    Vector3Int gridPosition;

    void Awake()
    {
        entity = GetComponent<Entity>();
        // entity.OnSelected += () => showMoveTargetActive(true);
        // entity.OnDeselected += () => showMoveTargetActive(false);
        entity.onDisable += () => removeMovingTarget(true);

        navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        navMeshAgent.speed = movingData.MovementSpeed;
        navMeshAgent.acceleration = movingData.Acceleration;
        navMeshAgent.angularSpeed = movingData.AngularSpeed;
    }

    void showMoveTargetActive(bool active)
    {
        if (moveIndicator != null)
            moveIndicator.SetActive(active && entity.IsEnabled);
    }

    void removeMovingTarget(bool performDestroy)
    {
        movingTarget = null;

        if (performDestroy)
            Destroy(moveIndicator);
    }

    void Start()
    {
        gridPosition = GridPlane.singleton.Grid.WorldToCell(transform.position);
        GridPlane.singleton.Spawn(gridPosition, entity);
    }

    public void MoveTo(Vector3 dest)
    {
        if (!entity.IsEnabled)
            return;

        navMeshAgent.SetDestination(dest);
        navMeshAgent.stoppingDistance = movingData.StoppingDistance;
        updateMoveIndicator(dest);
    }

    void updateMoveIndicator(Vector3 dest)
    {
        moveIndicator = Game.InstantiateOrMove(
            movingData.MoveIndicatorPrefab,
            moveIndicator,
            dest
        );
    }

    // Set a target transform to move towards, essentially following
    // that transform.
    public void MoveTo(MovingTarget newTarget)
    {
        movingTarget = newTarget;
        navMeshAgent.stoppingDistance = newTarget.closeEnoughDistance;

        // Remove the target if it is disabled and that is still the target.
        newTarget.entity.onDisable +=
            movingTarget.HasValue
            && movingTarget.Value.entity == newTarget.entity
                ? () => removeMovingTarget(false)
                : () => { };
    }

    void FixedUpdate()
    {
        if (!entity.IsEnabled)
            return;

        if (movingTarget.HasValue)
        {
            navMeshAgent.SetDestination(
                movingTarget.Value.entity.transform.position
            );
            updateMoveIndicator(movingTarget.Value.entity.transform.position);
        }

        SetMoving(navMeshAgent.velocity.magnitude > 0);

        updateGrid();
    }

    void updateGrid()
    {
        Vector3Int newGridPos = GridPlane.singleton.Grid.WorldToCell(
            transform.position
        );

        if (GridPlane.singleton.Move(gridPosition, newGridPos, entity))
            gridPosition = newGridPos;
    }

    // Set the animator `isMoving` param, enabling the Run animation.
    void SetMoving(bool moving)
    {
        if (entity.animator != null)
            entity.animator.SetBool("isMoving", moving);

        showMoveTargetActive(moving);
    }
}

public struct MovingTarget
{
    public readonly Entity entity;
    public readonly float closeEnoughDistance;

    public MovingTarget(Entity entity, float closeEnoughDistance)
    {
        this.entity = entity;
        this.closeEnoughDistance = closeEnoughDistance;
    }
}
