using UnityEngine;

public enum EntityActor
{
    enemy,
    player,
}

[CreateAssetMenu(
    fileName = "EntityData",
    menuName = "Scriptable Objects/EntityData"
)]
public class EntityData : ScriptableObject
{
    [SerializeField]
    private EntityActor actor = EntityActor.enemy;

    public EntityActor Actor => actor;
}
