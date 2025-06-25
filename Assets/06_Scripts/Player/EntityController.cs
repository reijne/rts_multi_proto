using System;
using System.Collections.Generic;
using System.Linq;
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

    Vector3 getGridOffset(int gridSize, int indexInSelection)
    {
        int row = indexInSelection / gridSize;
        int col = indexInSelection % gridSize;

        float offsetX = col - (gridSize - 1) / 2f;
        float offsetZ = row - (gridSize - 1) / 2f;
        Vector3 offset = new Vector3(offsetX, 0, offsetZ);
        return offset;
    }

    void moveSelectedEntities(Vector3 hit)
    {
        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(selection.Count));
        performActionOnSelection(
            (entity, idx) =>
            {
                MovingEntity movingEntity = entity.GetComponent<MovingEntity>();

                if (movingEntity == null)
                    return;

                movingEntity.MoveTo(hit + getGridOffset(gridSize, idx));
            }
        );
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
