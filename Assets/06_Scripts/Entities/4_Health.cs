using UnityEngine;

public class Health : MonoBehaviour
{
    public HealthData healthData;
    Entity entity;

    public float currentHealth { get; private set; }

    void Start()
    {
        currentHealth = healthData.Health;
        entity = GetComponent<Entity>();
        HealthBarDisplay.singleton.Add(entity);
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
        HealthBarDisplay.singleton.Remove(entity);
        Destroy(gameObject, 10f);
    }
}
