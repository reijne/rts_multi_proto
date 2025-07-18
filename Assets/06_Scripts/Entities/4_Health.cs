using UnityEngine;

public class Health : MonoBehaviour
{
    public HealthData healthData;

    private float currentHealth;

    void Start()
    {
        currentHealth = healthData.Health;
    }

    public void Damage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // TODO: trigger death animation, for this, move animator to Entity.
        Destroy(gameObject);
    }
}
