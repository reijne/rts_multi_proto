using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static EnemyController singleton { get; private set; }

    [SerializeField]
    GameObject enemyPrefab;

    List<Entity> enemies = new List<Entity>();

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);

        if (enemyPrefab == null)
            throw new Exception("EnemyController: requires an enemy prefab.");
    }

    public void Add(Entity entity)
    {
        enemies.Add(entity);
        Moving movingEntity = entity.GetComponent<Moving>();
        if (movingEntity != null)
        {
            // TODO: Figure out spawning of enemies and where they should go.
            // movingEntity.MoveTo(new Vector3(0, 0, 0));
        }
    }

    public void Remove(Entity entity)
    {
        enemies.Remove(entity);
    }

    private void spawnEnemy(Vector3 minimum, Vector3 maximum)
    {
        // Pick a random cell coordinate
        int randX = UnityEngine.Random.Range(0, GridPlane.singleton.GridSize.x);
        int randZ = UnityEngine.Random.Range(0, GridPlane.singleton.GridSize.y);

        Vector3Int cell = new Vector3Int(randX, 0, randZ);
        Vector3 spawnPosition = GridPlane.singleton.Grid.CellToWorld(cell);

        Debug.Log($"Spawning enemy on : {spawnPosition}");
        GameObject newEnemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        enemies.Add(newEnemy.GetComponent<Entity>());
    }

    private void spawnEnemies(int amountOfSpawns)
    {
        // First and last valid cell positions
        Vector3Int minCell = new Vector3Int(0, 0, 0);
        Vector3Int maxCell = new Vector3Int(
            GridPlane.singleton.GridSize.x - 1,
            0,
            GridPlane.singleton.GridSize.y - 1
        );

        Vector3 minWorld = GridPlane.singleton.Grid.CellToWorld(minCell);
        Vector3 maxWorld = GridPlane.singleton.Grid.CellToWorld(maxCell);

        for (int i = 0; i < amountOfSpawns; i++)
        {
            spawnEnemy(minWorld, maxWorld);
        }
    }

    private void destroyEnemies()
    {
        Debug.Log($"destroyEnemies, enemies.count: {enemies.Count}");
        int enemiesCount = enemies.Count;
        for (int i = 0; i < enemiesCount; i++)
        {
            Destroy(enemies[i].gameObject);
        }
        enemies.Clear();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            spawnEnemies(10);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            spawnEnemies(20);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            spawnEnemies(30);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log($"Alpha0 pressed");
            destroyEnemies();
        }
    }

    void moveEnemies(PopulatedFlowField flowField)
    {
        performActionOnEnemies(
            (enemy, _) =>
            {
                Vector3 moveDirection = flowField.GetDirection(
                    enemy.transform.position
                );
                // enemy.linearVelocity = moveDirection * 5;
            }
        );
    }

    void FixedUpdate() { }

    void performActionOnEnemies(Action<Entity, int> performAction)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            performAction(enemies[i], i);
        }
    }
}
