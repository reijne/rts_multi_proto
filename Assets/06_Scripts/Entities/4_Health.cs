using System;
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
            Die();
            return;
        }
        if (entity.entityData.Actor == EntityActor.player)
            Debug.Log("GetHit");

        if (entity.animator != null)
            entity.animator.SetTrigger("GetHit");
    }

    void Die()
    {
        isAlive = false;

        if (entity.entityData.Actor == EntityActor.player)
            Debug.Log("Death");

        if (entity.animator != null)
            entity.animator.Play("Death", 0);

        EntityController.singleton.Remove(entity);
        Destroy(gameObject, 10f);
    }
}
