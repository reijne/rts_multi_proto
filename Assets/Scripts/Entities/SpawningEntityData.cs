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
}
