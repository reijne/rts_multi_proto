using UnityEngine;

public class Moving : MonoBehaviour
{
    public MovingData movingData;
    private Entity entity;

    Vector3? moveTarget;
    Maybe<Transform> transformTarget = Maybe<Transform>.nothing;

    Vector3? target =>
        moveTarget != null
            ? moveTarget
            : transformTarget.caseOf<Vector3?>(t => t.position, () => null);

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
        moveTarget = newTarget;
    }

    public void MoveTo(Transform target)
    {
        transformTarget = Maybe<Transform>.of(target);
    }

    void Update()
    {
        step();
    }

    void step()
    {
        if (target == null)
        {
            SetMoving(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, moveTarget.Value);

        if (distance <= movingData.CloseEnoughDistance)
        {
            SetMoving(false);
            return;
        }

        SetMoving(true);
        Vector3 direction = (moveTarget.Value - transform.position).normalized;
        transform.position +=
            direction * movingData.MovementSpeed * Time.deltaTime;
    }

    // Set the animator `isMoving` param.
    void SetMoving(bool moving)
    {
        entity.animator.ifJust(a => a.SetBool("isMoving", moving));
    }
}
