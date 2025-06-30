using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static EnemyController singleton { get; private set; }

    public List<Entity> entities = new List<Entity>();

    public void Add(Entity entity)
    {
        entities.Add(entity);
        MovingEntity movingEntity = entity.GetComponent<MovingEntity>();
        if (movingEntity != null)
        {
            movingEntity.MoveTo(new Vector3(0, 0, 0));
        }
    }

    public void Remove(Entity entity)
    {
        entities.Remove(entity);
    }

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update() { }

    void performActionOnEntities(Action<Entity, int> performAction)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            performAction(entities[i], i);
        }
    }
}
