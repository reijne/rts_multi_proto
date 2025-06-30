using UnityEngine;

public class MovingEntity : MonoBehaviour
{
    public MovingEntityData movingEntityData;
    Animator animator;

    Vector3? moveTarget;
    Maybe<Transform> transformTarget = Maybe<Transform>.Nothing;

    Vector3? target =>
        moveTarget != null
            ? moveTarget
            : transformTarget.CaseOf<Vector3?>(t => t.position, () => null);

    void Start()
    {
        animator = GetComponent<Animator>();
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
        transformTarget = Maybe<Transform>.Of(target);
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

        if (distance <= movingEntityData.CloseEnoughDistance)
        {
            SetMoving(false);
            return;
        }

        SetMoving(true);
        Vector3 direction = (moveTarget.Value - transform.position).normalized;
        transform.position +=
            direction * movingEntityData.MovementSpeed * Time.deltaTime;
    }

    void SetMoving(bool moving)
    {
        // TODO: Once all things that move are animated, remove this and just inline, safe the if statement.
        if (animator != null)
        {
            animator.SetBool("isMoving", moving);
        }
    }
}
