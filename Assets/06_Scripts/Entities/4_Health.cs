using UnityEngine;

public class Health : MonoBehaviour
{
    public HealthData healthData;
    private Entity entity;

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

    public void GetHit(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            entity.animator.ifJust(a => a.SetTrigger("GetHit"));
        }
    }

    void Die()
    {
        entity.animator.ifJust(a => a.SetTrigger("Die"));
        EntityController.singleton.Remove(entity);
        Destroy(gameObject, 2f);
    }
}
