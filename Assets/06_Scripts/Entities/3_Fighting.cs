using UnityEngine;

public class Fighting : MonoBehaviour
{
    public FightingData fightingData;
    private Entity entity;

    void Start()
    {
        entity = GetComponent<Entity>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            entity.animator.ifJust(a => a.SetTrigger("Attack"));
        }
        // TODO: Figure out if this can ever be performant enough for many bois.
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            fightingData.Range
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            Entity targetEntity = hit.GetComponent<Entity>();
            if (targetEntity == null)
                continue;

            // Check for opposing team
            if (targetEntity.entityData.Actor == entity.entityData.Actor)
                continue;

            // Deal damage
            Health targetHealth = targetEntity.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.GetHit(fightingData.Damage);
                break; // Attack one target per frame (simple MVP)
            }
        }
    }
}
