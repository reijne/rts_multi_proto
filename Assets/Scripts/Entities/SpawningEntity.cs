using System;
using UnityEngine;

public class SpawningEntity : MonoBehaviour
{
    public SpawningEntityData spawningEntityData;
    public GameObject spawnPointIndicator;
    public GameObject prefab;
    public event Action OnSelected;
    public event Action OnDeselected;

    private Entity entity;

    // Location where units will move towards upon spawning.
    private Vector3 spawnPosition;
    private float timeOfLastSpawn;
    private int UnitQueue = 0;

    void Start()
    {
        timeOfLastSpawn = Time.time;
        entity = GetComponent<Entity>();
        spawnPosition = transform.position;

        entity.OnSelected += showSpawnPoint;
        entity.OnDeselected += hideSpawnPoint;

        // Hide it initially
        spawnPointIndicator.SetActive(false);
    }

    void showSpawnPoint() => spawnPointIndicator.SetActive(true);

    void hideSpawnPoint() => spawnPointIndicator.SetActive(false);

    void Update()
    {
        handleMouse();
        handleKeyboard();
        spawn();
    }

    void handleMouse()
    {
        if (entity.IsSelected && Input.GetMouseButtonDown(1))
        {
            Game.singleton.GetHit()
                .ifJust(hit =>
                {
                    spawnPosition = hit;
                    spawnPointIndicator.transform.position = hit;
                });
        }
    }

    void handleKeyboard()
    {
        if (
            entity.IsSelected
            && Input.GetKeyDown(KeyCode.Space)
            && ResourceController.singleton.TrySpendEnergy(
                spawningEntityData.UnitCost
            )
        )
        {
            AddUnitToQueue();
        }
    }

    void spawn()
    {
        if (
            UnitQueue <= 0
            || timeOfLastSpawn + spawningEntityData.Cooldown > Time.time
        )
            return;

        timeOfLastSpawn = Time.time;
        UnitQueue -= 1;
        ResourceController.singleton.DecrementGlobalQueue();

        Vector3 direction = (spawnPosition - transform.position).normalized;
        // Ensure we do not spawn inside the building, just pick "forward".
        if (direction == Vector3.zero)
            direction = transform.forward;

        // Half extents of the building in world space
        Vector3 halfExtents = transform.localScale / 2f;

        // Build the offset: only use X and Z, match Y with current position
        Vector3 spawnOffset = new Vector3(
            direction.x * halfExtents.x,
            0f,
            direction.z * halfExtents.z
        );

        // Final spawn point is the building position + offset
        Vector3 instantiatePosition = transform.position + spawnOffset;

        // Match Y with the building's base Y
        instantiatePosition.y = transform.position.y;

        GameObject ent = Instantiate(
            prefab,
            instantiatePosition,
            Quaternion.identity
        );
        MovingEntity movEnt = ent.GetComponent<MovingEntity>();
        movEnt.MoveTo(spawnPosition);
    }

    public void SpawnPosition(Vector3 pos)
    {
        spawnPosition = pos;
    }

    public void AddUnitToQueue()
    {
        UnitQueue += 1;
        ResourceController.singleton.IncrementGlobalQueue();
    }
}
