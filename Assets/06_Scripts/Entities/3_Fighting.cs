using UnityEngine;

public class Fighting : MonoBehaviour
{
    public FightingData fightingData;
    private Entity selfEntity;

    void Start()
    {
        selfEntity = GetComponent<Entity>();
    }

    void Update()
    {
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
            if (targetEntity.entityData.Actor == selfEntity.entityData.Actor)
                continue;

            // Deal damage
            Health targetHealth = targetEntity.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.Damage(fightingData.Damage);
                break; // Attack one target per frame (simple MVP)
            }
        }
    }
}
