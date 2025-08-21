using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public class Fighting : MonoBehaviour
{
    public FightingData fightingData;
    private Entity entity;
    private Moving moving;
    private float lastAttackTime = Mathf.NegativeInfinity;
    private Health currentTargetEnemy;

    private bool drawGizmos = false;

    void Start()
    {
        entity = GetComponent<Entity>();

        // Possible attributes.
        moving = GetComponent<Moving>();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        // Debug: show attack range of the fighter.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fightingData.Ranges.attack);

        // Debug: show vision of the fighter.
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, fightingData.Ranges.vision);
    }

    private float nextCheckTime = 1.5f;

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.G))
        //     drawGizmos = !drawGizmos;

        if (!entity.IsEnabled || Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + Random.Range(1f, 1.5f);

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
            && currentTargetEnemy.entity.IsEnabled
            && isWithinVision(currentTargetEnemy)
        )
        {
            // return;
        }

        currentTargetEnemy = getClosetEnemyInSight();
    }

    Health getClosetEnemyInSight()
    {
        ReadOnlyArray<Entity> inRange = GridPlane.singleton.GetEntitiesInRange(
            transform.position,
            fightingData.Ranges.vision
        );

        Health closestEnemy = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < inRange.Count; i++)
        {
            Health enemy = inRange[i].health;
            if (enemy == null || !enemy.isValidTargetFor(entity))
                continue;

            float distanceToEnemy = distanceTo(enemy);
            if (distanceToEnemy < closestDistance)
            {
                closestEnemy = enemy;
                closestDistance = distanceToEnemy;
            }
        }

        return closestEnemy;
    }

    float getAttackRange()
    {
        return entity.size + fightingData.Ranges.attack;
    }

    void attack()
    {
        if (currentTargetEnemy == null)
            return;

        float attackRange = getAttackRange();
        if (distanceTo(currentTargetEnemy) > attackRange)
        {
            if (moving != null)
                moving.MoveTo(
                    new MovingTarget(
                        currentTargetEnemy.entity,
                        // TODO: Make this into actual value instead of magic number?
                        // Idea is we move slightly close than actual attack range so we have
                        // time to attack in case someone moves.
                        attackRange * 0.9f
                    )
                );
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
