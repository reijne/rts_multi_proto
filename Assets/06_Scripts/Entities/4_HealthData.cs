using UnityEngine;

[CreateAssetMenu(
    fileName = "HealthData",
    menuName = "Scriptable Objects/HealthData"
)]
public class HealthData : ScriptableObject
{
    [SerializeField]
    private float health = 100f;
    public float Health => health;
}
