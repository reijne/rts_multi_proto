using System;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public EntityData entityData;
    public event Action OnSelected;
    public event Action OnDeselected;
    private bool isSelected = false;

    public bool IsSelected => isSelected;

    Collider selectCollider;

    void Start()
    {
        if (entityData.Actor == EntityActor.player)
        {
            EntityController.singleton.Add(this);
        }
        else if (entityData.Actor == EntityActor.enemy)
        {
            EnemyController.singleton.Add(this);
        }
        selectCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Get this entities renderer bounds in screen space.
    ///
    /// So get the rectangle on the screen that surrounds this entity.
    /// Essentially drawing a little rectangle around the entity entirely.
    /// </summary>
    public Maybe<Rect> GetScreenBoundsRect()
    {
        Bounds bounds = selectCollider.bounds;

        // Convert the 8 corners of the bounding box to screen space
        Vector3[] corners = new Vector3[8];
        Vector3 entMin = bounds.min;
        Vector3 entMax = bounds.max;

        corners[0] = Camera.main.WorldToScreenPoint(
            new Vector3(entMin.x, entMin.y, entMin.z)
        );
        corners[1] = Camera.main.WorldToScreenPoint(
            new Vector3(entMax.x, entMin.y, entMin.z)
        );
        corners[2] = Camera.main.WorldToScreenPoint(
            new Vector3(entMin.x, entMax.y, entMin.z)
        );
        corners[3] = Camera.main.WorldToScreenPoint(
            new Vector3(entMin.x, entMin.y, entMax.z)
        );
        corners[4] = Camera.main.WorldToScreenPoint(
            new Vector3(entMax.x, entMax.y, entMin.z)
        );
        corners[5] = Camera.main.WorldToScreenPoint(
            new Vector3(entMin.x, entMax.y, entMax.z)
        );
        corners[6] = Camera.main.WorldToScreenPoint(
            new Vector3(entMax.x, entMin.y, entMax.z)
        );
        corners[7] = Camera.main.WorldToScreenPoint(
            new Vector3(entMax.x, entMax.y, entMax.z)
        );

        // Find the screen-space bounding box of those points
        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minY = corners[0].y;
        float maxY = corners[0].y;

        for (int cornerIdx = 0; cornerIdx < 8; cornerIdx++)
        {
            Vector3 corner = corners[cornerIdx];
            if (corner.z < 0)
                continue; // Behind the camera
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minY = Mathf.Min(minY, corner.y);
            maxY = Mathf.Max(maxY, corner.y);
        }
        return Maybe<Rect>.Of(
            new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY)
        );
    }

    public void Select()
    {
        isSelected = true;
        GetComponentInChildren<Renderer>().material.color = Color.green;
        OnSelected?.Invoke();
    }

    public void Deselect()
    {
        isSelected = false;
        GetComponentInChildren<Renderer>().material.color = Color.white;
        OnDeselected?.Invoke();
    }
}
