using System;
using UnityEngine;

public class Spawning : MonoBehaviour
{
    public SpawningData spawningEntityData;
    public GameObject spawnPointIndicator;
    public GameObject prefab;

    public event Action OnSelected;
    public event Action OnDeselected;

    // Private attributes that are guaranteed to exist.
    private Entity entity;

    float timeOfLastSpawn;
    int UnitQueue = 0;

    void Awake()
    {
        // Hide it initially
        // spawnPointIndicator.SetActive(false);
    }

    void Start()
    {
        timeOfLastSpawn = Time.time;
        entity = GetComponent<Entity>();

        entity.OnSelected += showSpawnPoint;
        entity.OnDeselected += hideSpawnPoint;
    }

    void showSpawnPoint() => spawnPointIndicator.SetActive(true);

    void hideSpawnPoint() => spawnPointIndicator.SetActive(false);

    void Update()
    {
        handleMouse();
        handleKeyboard();
    }

    void FixedUpdate()
    {
        spawn();
    }

    void handleMouse()
    {
        if (entity.IsSelected && Input.GetMouseButtonDown(1))
            Game.GetHit()
                .ifJust(hit =>
                {
                    spawnPointIndicator.transform.position = hit;
                });
    }

    public void AddUnitToQueue(int amount)
    {
        UnitQueue += amount;
        ResourceController.singleton.IncrementGlobalQueue(amount);
    }

    void handleKeyboard()
    {
        if (!entity.IsEnabled || !entity.IsSelected)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) { }

        if (
            Input.GetKeyDown(KeyCode.Q)
            && Input.GetKey(KeyCode.LeftShift)
            && ResourceController.singleton.TrySpendEnergy(
                spawningEntityData.UnitCost * 5
            )
        )
        {
            AddUnitToQueue(50);
        }
        else if (
            Input.GetKeyDown(KeyCode.Q)
            && ResourceController.singleton.TrySpendEnergy(
                spawningEntityData.UnitCost
            )
        )
        {
            AddUnitToQueue(10);
        }
    }

    void spawn()
    {
        if (
            UnitQueue <= 0
            || timeOfLastSpawn + spawningEntityData.Cooldown > Time.time
        )
            return;

        unsafeSpawn();
    }

    /// <summary> Spawn 10 units </summary>
    void unsafeSpawn()
    {
        for (int i = 0; i < 10; i++)
        {
            timeOfLastSpawn = Time.time;
            UnitQueue -= 1;
            ResourceController.singleton.DecrementGlobalQueue();

            Vector3 direction = (
                spawnPointIndicator.transform.position - transform.position
            ).normalized;

            // Ensure we do not spawn inside the building, just pick "forward".
            if (direction == Vector3.zero)
                direction = transform.forward;

            // Half extents of the building in world space
            Vector3 halfExtents = entity.collider.bounds.size / 1.8f;

            // Build the offset: only use X and Z, match Y with current position
            Vector3 spawnOffset = new Vector3(
                direction.x * halfExtents.x,
                transform.position.y, // Height of the building.
                direction.z * halfExtents.z
            );

            // Final spawn point is the building position + offset
            Vector3 instantiatePosition = transform.position + spawnOffset;

            // Match Y with the building's base Y
            instantiatePosition.y = 0;

            GameObject ent = Instantiate(
                prefab,
                instantiatePosition,
                Quaternion.identity
            );
            Moving movEnt = ent.GetComponent<Moving>();
            movEnt.MoveTo(spawnPointIndicator.transform.position);
        }
    }
}
