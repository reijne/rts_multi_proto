using UnityEngine;

[CreateAssetMenu(
    fileName = "SpawningEntityData",
    menuName = "Scriptable Objects/SpawningEntityData"
)]
public class SpawningEntityData : ScriptableObject
{
    [SerializeField]
    private float cooldown = 1f;

    public float Cooldown => cooldown;

    [SerializeField]
    private int unitCost = 1;

    public int UnitCost => unitCost;
}
