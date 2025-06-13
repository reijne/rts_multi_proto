using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityController : MonoBehaviour
{
    private Collider[] selection = new Collider[0];
    private Vector2? mouseDown;
    private Vector2? mouseUp;

    public float selectionHeight = 1f;

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
            performActionOnSelection(collider =>
            {
                MovingEntity movingEntity =
                    collider.GetComponent<MovingEntity>();

                if (movingEntity == null)
                    return;

                RaycastHit hit;
                if (
                    !Physics.Raycast(
                        Camera.main.ScreenPointToRay(Input.mousePosition),
                        out hit,
                        100f
                    )
                )
                    return;

                movingEntity.MoveTo(hit.point);
            });
        }
    }

    void handleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            deselect();
            selection = new Collider[0];
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
        if (mouseDown == null || mouseUp == null)
            return;

        Vector2 min = Vector2.Min(mouseDown.Value, mouseUp.Value);
        Vector2 max = Vector2.Max(mouseDown.Value, mouseUp.Value);

        mouseDown = null;
        mouseUp = null;

        // Sample world points at rectangle corners (assuming a flat ground plane at y = 0)
        Vector3 p1 = ScreenToWorldOnPlane(min);
        Debug.Log("World p1:" + p1);

        Vector3 p2 = ScreenToWorldOnPlane(max);
        Debug.Log("World p2:" + p2);

        Vector3 center = (p1 + p2) / 2f;
        Debug.Log("center:" + center);

        Vector3 halfExtents = new Vector3(
            Mathf.Abs(p2.x - p1.x) / 2f,
            selectionHeight / 2f,
            Mathf.Abs(p2.z - p1.z) / 2f
        );
        Debug.Log("halfExtents" + halfExtents);

        deselect();

        selection = Physics.OverlapBox(
            center + Vector3.up * (selectionHeight / 2f),
            halfExtents
        );

        select();
    }

    void performActionOnSelection(Action<Collider> performAction)
    {
        Debug.Log("performAction selection" + selection);
        Debug.Log("selection.length" + selection.Length);
        for (int i = 0; i < selection.Length; i++)
        {
            performAction(selection[i]);
        }
    }

    void performActionOnSelectedEntity(Action<Entity> performAction)
    {
        performActionOnSelection(collider =>
        {
            Entity entity = collider.gameObject.GetComponent<Entity>();
            if (entity != null)
                performAction(entity);
        });
    }

    void deselect()
    {
        performActionOnSelectedEntity(entity => entity.Deselect());
    }

    void select()
    {
        performActionOnSelectedEntity(entity => entity.Select());
    }

    Vector3 ScreenToWorldOnPlane(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero); // Horizontal plane at y=0

        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }
}
