using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityController : MonoBehaviour
{
    public static EntityController singleton { get; private set; }

    [SerializeField]
    /// <summary> The height of the box we select entities in. </summary>
    private float selectionHeight;

    public List<Entity> entities = new List<Entity>();

    // Private attributes that are guaranteed to exist.
    private List<Entity> selection = new List<Entity>();

    Vector2? mouseDown;
    Vector2? mouseUp;

    /// <summary> Add an `Entity` to the entire list of entities in this controller. </summary>
    public void Add(Entity entity)
    {
        entities.Add(entity);
    }

    /// <summary> Remove an `Entity` to the entire list of entities in this controller. </summary>
    public void Remove(Entity entity)
    {
        entities.Remove(entity);
        selection.Remove(entity);
    }

    /// <summary> Set this controller to be a singleton, without destroy on load. </summary>
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
    }

    void OnGUI()
    {
        showSelectionBox();
    }

    void handleMouse()
    {
        captureMousePositions();
        handleMouseRightClick();

        // TODO: Select on double click.
        performMouseSelection();
    }

    void captureMousePositions()
    {
        if (Input.GetMouseButtonDown(0))
            mouseDown = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
            mouseUp = Input.mousePosition;
    }

    void handleMouseRightClick()
    {
        if (Input.GetMouseButtonDown(1) && selection.Count > 0)
            Game.GetHit().ifJust(moveSelectedEntities);
    }

    void moveSelectedEntities(Vector3 hit)
    {
        Vector3 desiredLocation = GridPlane.singleton.Grid.GetCellCenterWorld(
            GridPlane.singleton.Grid.WorldToCell(hit)
        );
        int selectionCount = selection.Count;
        List<Moving> movingSelection = new List<Moving>();

        for (int e = 0; e < selectionCount; e++)
        {
            Entity ent = selection[e];

            if (ent.moving == null)
                continue;

            movingSelection.Add(ent.moving);
        }

        // We have no actual "moving" entities selected, abort mission!
        if (movingSelection.Count == 0)
            return;

        moveSelectionInFormation(desiredLocation, movingSelection);
    }

    Vector3 getFormationOffset(
        int formationSize,
        int indexInSelection
    // TODO: Update formation for unit size.
    // Vector2 unitSize
    )
    {
        int row = indexInSelection / formationSize;
        int col = indexInSelection % formationSize;

        Vector3 formationCellSize = GridPlane.singleton.cellSize;
        float offsetX = (col - (formationSize - 1) / 2f) * formationCellSize.x;
        float offsetZ = (row - (formationSize - 1) / 2f) * formationCellSize.z;
        return new Vector3(offsetX, 0, offsetZ);
    }

    void moveSelectionInFormation(Vector3 hit, List<Moving> movingSelection)
    {
        Vector3[] destinations = new Vector3[movingSelection.Count];
        int formationSize = Mathf.CeilToInt(Mathf.Sqrt(movingSelection.Count));

        Vector3 max =
            hit + getFormationOffset(formationSize, movingSelection.Count - 1);

        Vector3 min = hit + getFormationOffset(formationSize, 0);
        for (int i = 0; i < movingSelection.Count; i++)
        {
            destinations[i] = hit + getFormationOffset(formationSize, i);
            max = Vector3.Max(max, movingSelection[i].transform.position);
            min = Vector3.Min(min, movingSelection[i].transform.position);
        }

        Vector3Int maxInt = GridPlane.singleton.Grid.WorldToCell(max);
        Vector3Int minInt = GridPlane.singleton.Grid.WorldToCell(min);

        GridPlane.singleton.DebugHighlightCellBox(maxInt, Color.white);
        GridPlane.singleton.DebugHighlightCellBox(minInt, Color.white);

        for (int i = 0; i < movingSelection.Count; i++)
        {
            movingSelection[i]
                .MoveWith(
                    GridPlane.singleton.flowField.Create(
                        destinations[i],
                        new Vector2Int(minInt.x - 1, minInt.z - 1),
                        new Vector2Int(maxInt.x + 1, maxInt.z + 1)
                    ),
                    destinations[i]
                );
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

    /// <summary> Select entities inside the box dragged with the cursor. </summary>
    void performMouseSelection()
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
            Rect entityScreenRect = entity.GetScreenBoundsRect();
            if (entityScreenRect == null)
                continue;

            // Here we also allow inverse, meaning dragging direction does not matter.
            if (entityScreenRect.Overlaps(selectionRect, true))
                selection.Add(entity);
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
