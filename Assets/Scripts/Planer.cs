using UnityEngine;

public class Planer : MonoBehaviour
{
    public Grid grid;
    private Material material;

    void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        float scaleX = transform.localScale.x / grid.cellSize.x;
        float scaleY = transform.localScale.z / grid.cellSize.z;
        material.mainTextureScale = new Vector2(scaleX, scaleY);
    }

    void OnMouseDown()
    {
        Game.singleton.GetHit()
            .ifJust(hit =>
            {
                Vector3Int cell = grid.WorldToCell(hit);
                DebugHighlightCellBox(cell);
            });
    }

    /// <summary>
    /// Draw the outline of a cell using Debug.DrawLine.
    /// </summary>
    void DebugHighlightCellBox(Vector3Int cell)
    {
        Vector3 center = grid.GetCellCenterWorld(cell);
        float halfWidth = grid.cellSize.x / 2f;
        float halfDepth = grid.cellSize.z / 2f;
        float y = 0.1f;

        Vector3 bl = center + new Vector3(-halfWidth, y, -halfDepth); // bottom-left
        Vector3 br = center + new Vector3(halfWidth, y, -halfDepth); // bottom-right
        Vector3 tr = center + new Vector3(halfWidth, y, halfDepth); // top-right
        Vector3 tl = center + new Vector3(-halfWidth, y, halfDepth); // top-left

        Debug.DrawLine(bl, br, Color.green, 3f);
        Debug.DrawLine(br, tr, Color.green, 3f);
        Debug.DrawLine(tr, tl, Color.green, 3f);
        Debug.DrawLine(tl, bl, Color.green, 3f);
    }
}
