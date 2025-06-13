using UnityEngine;

public class MovingEntity : MonoBehaviour
{
    public MovingEntityData movingEntityData;

    Vector3? target;

    public void MoveTo(Vector3 destination)
    {
        target = new Vector3(
            destination.x,
            transform.position.y,
            destination.z
        );
    }

    void Update()
    {
        step();
    }

    void step()
    {
        if (
            target == null
            || Vector3.Distance(transform.position, target.Value)
                <= movingEntityData.CloseEnoughDistance
        )
            return;

        Vector3 direction = (target.Value - transform.position).normalized;
        transform.position +=
            direction * movingEntityData.MovementSpeed * Time.deltaTime;
    }
}
