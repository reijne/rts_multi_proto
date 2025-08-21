using UnityEngine;

[CreateAssetMenu(
    fileName = "SpawningData",
    menuName = "Scriptable Objects/SpawningData"
)]
public class SpawningData : ScriptableObject
{
    [SerializeField]
    private float cooldown = 1f;

    public float Cooldown => cooldown;

    [SerializeField]
    private int unitCost = 1;

    public int UnitCost => unitCost;

    [SerializeField]
    private GameObject spawnPointIndicatorPrefab;

    public GameObject SpawnPointIndicatorPrefab => spawnPointIndicatorPrefab;

    [SerializeField]
    private GameObject entityPrefab;

    public GameObject EntityPrefab => entityPrefab;
}
