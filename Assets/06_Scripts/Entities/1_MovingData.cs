using UnityEngine;

[CreateAssetMenu(
    fileName = "MovingData",
    menuName = "Scriptable Objects/MovingData"
)]
public class MovingData : ScriptableObject
{
    [SerializeField]
    private float movementSpeed = 1f;

    // Speed at which the entity moves per delta time.
    public float MovementSpeed => movementSpeed;

    [SerializeField]
    private float closeEnoughDistance = 1f;

    // Distance to stop moving when close enough to the target.
    public float CloseEnoughDistance => closeEnoughDistance;
}
