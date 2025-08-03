using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityController : MonoBehaviour
{
    public static EntityController singleton { get; private set; }

    public List<Entity> entities = new List<Entity>();
    private List<Entity> selection = new List<Entity>();
    private Vector2? mouseDown;
    private Vector2? mouseUp;

    public float selectionHeight;

    public void Add(Entity entity)
    {
        entities.Add(entity);
    }

    public void Remove(Entity entity)
    {
        entities.Remove(entity);
        selection.Remove(entity);
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

    void Update()
    {
        handleMouse();
        handleKeyboard();
        performSelection();
    }

    void OnGUI()
    {
        showSelectionBox();
    }

    void handleMouse()
    {
        captureMousePositions();
        handleRightClick();
    }

    void captureMousePositions()
    {
        if (Input.GetMouseButtonDown(0))
            mouseDown = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
            mouseUp = Input.mousePosition;
    }

    void handleRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Game.singleton.GetHit().ifJust(moveSelectedEntities);
        }
    }

    Vector3 getFormationOffset(int formationSize, int indexInSelection)
    {
        int row = indexInSelection / formationSize;
        int col = indexInSelection % formationSize;

        float offsetX = col - (formationSize - 1) / 2f;
        float offsetZ = row - (formationSize - 1) / 2f;
        Vector3 cellSize = GridPlane.singleton.CellSize;

        return new Vector3(offsetX * cellSize.x, 0, offsetZ * cellSize.z);
    }

    Vector3Int? GetClosestAvailableNotIn(
        HashSet<Vector3Int> reserved,
        Vector3Int start,
        int maxRadius
    )
    {
        if (!reserved.Contains(start))
            return start;
        for (int r = 1; r <= maxRadius; r++)
        for (int dx = -r; dx <= r; dx++)
        for (int dz = -r; dz <= r; dz++)
        {
            if (dx == 0 && dz == 0)
                continue;
            var c = new Vector3Int(start.x + dx, start.y, start.z + dz);
            if (!reserved.Contains(c))
                return c;
        }
        return null;
    }

    void moveSelectedEntities(Vector3 clickWorld)
    {
        var field = GridPlane.singleton.flowField.Create(clickWorld);
        int formationSize = Mathf.CeilToInt(Mathf.Sqrt(selection.Count));

        // Reserve unique slots
        var reserved = new HashSet<Vector3Int>();
        var slotWorlds = new List<Vector3>(selection.Count);

        for (int i = 0; i < selection.Count; i++)
        {
            Vector3 offset = getFormationOffset(formationSize, i);
            Vector3 desiredSlot = field.destination + offset;

            // Snap to nearest free cell (with lightweight reservation)
            var cell = GridPlane.singleton.WorldToCell(desiredSlot);
            Vector3Int best = cell;
            if (reserved.Contains(best))
            {
                // simple fallback to your spiral search
                var alt = GetClosestAvailableNotIn(reserved, cell, 5);
                if (alt.HasValue)
                    best = alt.Value;
            }
            reserved.Add(best);
            slotWorlds.Add(GridPlane.singleton.CellToWorld(best));
        }

        // Send each entity with its offset toward its slot
        for (int i = 0; i < selection.Count; i++)
        {
            var ent = selection[i];
            var slot = slotWorlds[i];

            if (ent.moving == null)
                continue;

            // Convert back to offset relative to the common DestinationWorld so your MoveWith(field, offset) still works
            Vector3 offset = slot - field.destination;
            ent.moving.MoveWith(field, offset);
        }
    }

    void handleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            deselect();
            selection.Clear();
        }
    }

    void showSelectionBox()
    {
        if (mouseDown == null || mouseUp != null)
            return;

        Vector2 currentMouse = Input.mousePosition;
        Vector2 start = mouseDown.Value;

        // Flip y axis for GUI (because GUI y=0 is at top of screen, Input y=0 is bottom)
        start.y = Screen.height - start.y;
        currentMouse.y = Screen.height - currentMouse.y;

        Vector2 topLeft = new Vector2(
            Mathf.Min(start.x, currentMouse.x),
            Mathf.Min(start.y, currentMouse.y)
        );

        Vector2 size = new Vector2(
            Mathf.Abs(start.x - currentMouse.x),
            Mathf.Abs(start.y - currentMouse.y)
        );

        Rect rect = new Rect(topLeft, size);
        GUI.Box(rect, GUIContent.none);
    }

    void performSelection()
    {
        if (mouseDown == null || mouseUp == null || entities.Count == 0)
            return;

        Vector2 min = Vector2.Min(mouseDown.Value, mouseUp.Value);
        Vector2 max = Vector2.Max(mouseDown.Value, mouseUp.Value);
        mouseDown = null;
        mouseUp = null;

        Rect selectionRect = new Rect(
            min.x,
            Screen.height - max.y,
            max.x - min.x,
            max.y - min.y
        );

        deselect();

        selection.Clear();
        for (int i = 0; i < entities.Count; i++)
        {
            Entity entity = entities[i];

            entity
                .GetScreenBoundsRect()
                .ifJust(entityScreenRect =>
                {
                    // Here we also allow negative overlap, meaning the
                    // selection is within the entities box.
                    if (entityScreenRect.Overlaps(selectionRect, true))
                    {
                        selection.Add(entity);
                    }
                });
        }
        select();
    }

    void performActionOnSelection(Action<Entity, int> performAction)
    {
        for (int i = 0; i < selection.Count; i++)
        {
            performAction(selection[i], i);
        }
    }

    void deselect()
    {
        performActionOnSelection((entity, _) => entity.Deselect());
    }

    void select()
    {
        performActionOnSelection((entity, _) => entity.Select());
    }
}
