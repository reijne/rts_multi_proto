using System;
using System.Collections.Generic;
using UnityEngine;

public class Fighting : MonoBehaviour
{
    public FightingData fightingData;
    private Entity entity;
    private Moving moving;
    private float lastAttackTime = Mathf.NegativeInfinity;
    private Health currentTargetEnemy;
    private CellType targetFilter;

    void Start()
    {
        entity = GetComponent<Entity>();

        // TODO: Expand for buildings as well.
        if (entity.entityData.Actor == EntityActor.player)
            targetFilter = CellType.EnemyUnit;
        else
            targetFilter = CellType.Unit;

        // Possible attributes.
        moving = GetComponent<Moving>();
    }

    // void OnDrawGizmos()
    // {
    //     // Debug: show attack range of the fighter.
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawWireSphere(transform.position, fightingData.Ranges.attack);

    //     // Debug: show vision of the fighter.
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawWireSphere(transform.position, fightingData.Ranges.vision);
    // }

    private float nextCheckTime = 1.5f;

    void Update()
    {
        if (!entity.Enabled || Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + UnityEngine.Random.Range(1f, 1.5f);

        // TODO: Fix this mess lol, dont attack your teammates ey.
        // updateClosestEnemy();
        // attack();
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

    Health getClosetEnemyInSight()
    {
        List<Entity> inRange = GridPlane.singleton.GetEntitiesInRange(
            transform.position,
            fightingData.Ranges.vision
        );

        Health closestEnemy = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < inRange.Count; i++)
        {
            Health enemy = inRange[i].health;
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

        if (entity.animator != null)
            entity.animator.SetTrigger("Attack");
    }
}
