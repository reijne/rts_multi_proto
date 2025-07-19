using UnityEngine;

public class Fighting : MonoBehaviour
{
    public FightingData fightingData;
    private Entity entity;
    private Moving moving;
    private Health health;
    private float lastAttackTime = Mathf.NegativeInfinity;
    private Health currentTargetEnemy;

    void Start()
    {
        entity = GetComponent<Entity>();

        // Possible attributes.
        moving = GetComponent<Moving>();
        health = GetComponent<Health>();
    }

    void OnDrawGizmos()
    {
        // Debug: show attack range of the fighter.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fightingData.Ranges.attack);

        // Debug: show vision of the fighter.
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, fightingData.Ranges.vision);
    }

    void Update()
    {
        if (health != null && !health.IsAlive)
            return;

        updateClosestEnemy();
        attack();
    }

    float distanceTo(Health target) =>
        Vector3.Distance(transform.position, target.transform.position);

    bool isWithinVision(Health target) =>
        distanceTo(target) <= fightingData.Ranges.vision;

    void updateClosestEnemy()
    {
        // TODO: Maybe remove this logic if its annoying, fighters
        // do not switch targets if they can still see their old one.
        // This could mean running one target around and having your
        // units chase em, instead of switching to the new closest one.
        if (
            currentTargetEnemy != null
            && currentTargetEnemy.IsAlive
            && isWithinVision(currentTargetEnemy)
        )
        {
            return;
        }

        currentTargetEnemy = getClosetEnemyInSight();
    }

    Health getHealth(Collider hit)
    {
        if (hit.gameObject == gameObject)
            return null;

        Entity targetEntity = hit.GetComponent<Entity>();
        if (targetEntity == null)
            return null;

        if (targetEntity.entityData.Actor == entity.entityData.Actor)
            return null;

        return targetEntity.GetComponent<Health>();
    }

    Health getClosetEnemyInSight()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            fightingData.Ranges.vision
        );

        Health closestEnemy = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            Health enemy = getHealth(hit);
            if (
                enemy != null
                && distanceTo(enemy) < closestDistance
                && enemy.IsAlive
            )
            {
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    void attack()
    {
        if (currentTargetEnemy == null)
            return;

        if (distanceTo(currentTargetEnemy) > fightingData.Ranges.attack)
        {
            if (moving != null)
                moving.MoveTo(currentTargetEnemy.transform);
            return;
        }

        if (Time.time - lastAttackTime < fightingData.Cooldown)
        {
            return;
        }

        transform.LookAt(currentTargetEnemy.transform);
        currentTargetEnemy.GetHit(fightingData.Damage);
        lastAttackTime = Time.time;

        if (entity.entityData.Actor == EntityActor.player)
            Debug.Log("Attack");

        if (entity.animator != null)
            entity.animator.SetTrigger("Attack");
    }
}
