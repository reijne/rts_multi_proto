using UnityEngine;

[CreateAssetMenu(
    fileName = "FightingData",
    menuName = "Scriptable Objects/FightingData"
)]
public class FightingData : ScriptableObject
{
    [SerializeField]
    private float damage = 25f;
    public float Damage => damage;

    [SerializeField]
    private float range = 5f;
    public float Range => range;
}
