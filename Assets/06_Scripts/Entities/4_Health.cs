using UnityEngine;

public class Health : MonoBehaviour
{
    public HealthData healthData;
    Entity entity;

    private float currentHealth;

    void Start()
    {
        currentHealth = healthData.Health;
        entity = GetComponent<Entity>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            GetHit(healthData.Health / 2);
        }
    }

    /// <summary> Can other target this entity, used to determine fighting target. </summary>
    public bool isValidTargetFor(Entity other)
    {
        if (!entity.IsEnabled)
            return false;

        return entity.entityData.Actor != other.entityData.Actor;
    }

    public void GetHit(float amount)
    {
        if (!entity.IsEnabled)
        {
            return;
        }

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            die();
            return;
        }

        if (entity.animator != null)
            entity.animator.SetTrigger("GetHit");
    }

    void die()
    {
        entity.IsEnabled = false;

        if (entity.animator != null)
            entity.animator.Play("Death", 0);

        EntityController.singleton.Remove(entity);
        Destroy(gameObject, 10f);
    }
}
