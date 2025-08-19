using UnityEngine;
using UnityEngine.AI;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    public GameObject moveIndicatorPrefab;
    public GameObject moveIndicator;

    public Entity entity { get; private set; }

    NavMeshAgent navMeshAgent;

    MovingTarget? movingTarget;

    Vector3Int gridPosition;

    void Awake()
    {
        entity = GetComponent<Entity>();
        entity.OnSelected += () => setMoveTargetActive(true);
        entity.OnDeselected += () => setMoveTargetActive(false);
        entity.onDisable += onDisabled;
        navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        navMeshAgent.speed = movingData.MovementSpeed;
        navMeshAgent.acceleration = movingData.Acceleration;
        navMeshAgent.angularSpeed = movingData.AngularSpeed;
    }

    void setMoveTargetActive(bool active)
    {
        if (moveIndicator != null)
            moveIndicator.SetActive(active && entity.IsEnabled);
    }

    void onDisabled()
    {
        setMoveTargetActive(false);
        movingTarget = null;
        navMeshAgent.isStopped = true;
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
        UpdateMoveIndicator(dest);
    }

    void UpdateMoveIndicator(Vector3 dest)
    {
        if (moveIndicator == null)
        {
            if (moveIndicatorPrefab != null)
                moveIndicator = Instantiate(
                    moveIndicatorPrefab,
                    dest,
                    Quaternion.identity
                );
        }
        else
            moveIndicator.transform.position = dest;
    }

    // Set a target transform to move towards, essentially following
    // that transform.
    public void MoveTo(Transform target, float closeEnoughDistance)
    {
        movingTarget = new MovingTarget(target, closeEnoughDistance);
        navMeshAgent.stoppingDistance = closeEnoughDistance;
    }

    void FixedUpdate()
    {
        if (!entity.IsEnabled)
            return;

        navMeshAgent.avoidancePriority = movingData.CollisionAvoidance;

        if (movingTarget.HasValue && movingTarget.Value.transform != null)
        {
            navMeshAgent.SetDestination(movingTarget.Value.transform.position);
            UpdateMoveIndicator(movingTarget.Value.transform.position);
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
