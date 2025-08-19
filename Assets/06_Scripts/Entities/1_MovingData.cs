using UnityEngine;

[CreateAssetMenu(
    fileName = "MovingData",
    menuName = "Scriptable Objects/MovingData"
)]
public class MovingData : ScriptableObject
{
    [SerializeField]
    private float movementSpeed = 5f;

    // Speed at which the entity moves per delta time.
    public float MovementSpeed => movementSpeed;

    [SerializeField]
    private float acceleration = 200f;

    // Speed at which the entity accelerates from standing still, or brakes.
    public float Acceleration => acceleration;

    [SerializeField]
    private float angularSpeed = 360f;

    // Speed at which the entity turns while moving.
    public float AngularSpeed => angularSpeed;

    [SerializeField]
    private float stoppingDistance = .1f;

    // Distance to stop moving when close enough to the target.
    public float StoppingDistance => stoppingDistance;

    [SerializeField]
    private int collisionAvoidance = 50;

    public int CollisionAvoidance => collisionAvoidance;
}
