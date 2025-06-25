using UnityEngine;

[CreateAssetMenu(
    fileName = "MovingEntityData",
    menuName = "Scriptable Objects/MovingEntityData"
)]
public class MovingEntityData : ScriptableObject
{
    [SerializeField]
    private float movementSpeed = 1f;

    [SerializeField]
    private float closeEnoughDistance = 1f;

    public float MovementSpeed => movementSpeed;
    public float CloseEnoughDistance => closeEnoughDistance;
    //   float health;
    //   float damage;
    //   float range;
}
