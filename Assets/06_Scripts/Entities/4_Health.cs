using UnityEngine;

public class Health : MonoBehaviour
{
    public HealthData healthData;
    private Entity entity;

    private float currentHealth;
    private bool isAlive = true;

    public bool IsAlive => isAlive;

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
        if (!isAlive)
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
        isAlive = false;

        if (entity.animator != null)
            entity.animator.Play("Death", 0);

        EntityController.singleton.Remove(entity);
        Destroy(gameObject, 10f);
    }
}
