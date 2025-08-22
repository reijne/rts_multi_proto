using System.Collections.Generic;
using UnityEngine;

public class EntitySelectionDisplay : MonoBehaviour
{
    public static EntitySelectionDisplay singleton;
    public Material baseMaterial;

    static Mesh boxMesh;
    static Mesh ringMesh;

    readonly List<Matrix4x4> matrices = new(1023);
    readonly Dictionary<Color32, Material> materials = new();

    Quaternion rotation = Quaternion.identity;

    const float innerDefault = 0.8f;
    const float outerDefault = 1f;

    Mesh makeRingOutlineMesh(
        float innerRadius = innerDefault,
        float outerRadius = outerDefault,
        int segments = 8
    )
    {
        var mesh = new Mesh { name = "SelectionRing" };

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Outer at i, Inner at i
            verts.Add(new Vector3(cos * outerRadius, 0f, sin * outerRadius)); // i0
            uvs.Add(new Vector2(t, 1f));
            verts.Add(new Vector3(cos * innerRadius, 0f, sin * innerRadius)); // i1
            uvs.Add(new Vector2(t, 0f));

            if (i < segments)
            {
                int i0 = 2 * i; // outer_i
                int i1 = 2 * i + 1; // inner_i
                int i2 = 2 * i + 2; // outer_{i+1}
                int i3 = 2 * i + 3; // inner_{i+1}

                // CCW when viewed from +Y (normals up)
                indices.Add(i0);
                indices.Add(i2);
                indices.Add(i3);
                indices.Add(i0);
                indices.Add(i3);
                indices.Add(i1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh makeBoxOutlineMesh(
        float innerHalf = innerDefault,
        float outerHalf = outerDefault
    )
    {
        // innerHalf/outerHalf = half-extents in X/Z (outer must be > inner)
        innerHalf = Mathf.Max(0f, Mathf.Min(innerHalf, outerHalf - 1e-4f));

        var m = new Mesh { name = "SelectionBoxOutline" };

        // Outer square (XZ)
        Vector3 o0 = new(-outerHalf, 0f, -outerHalf);
        Vector3 o1 = new(outerHalf, 0f, -outerHalf);
        Vector3 o2 = new(outerHalf, 0f, outerHalf);
        Vector3 o3 = new(-outerHalf, 0f, outerHalf);

        // Inner square (XZ)
        Vector3 i0 = new(-innerHalf, 0f, -innerHalf);
        Vector3 i1 = new(innerHalf, 0f, -innerHalf);
        Vector3 i2 = new(innerHalf, 0f, innerHalf);
        Vector3 i3 = new(-innerHalf, 0f, innerHalf);

        // Vert order: O0 O1 O2 O3  I0 I1 I2 I3
        var verts = new List<Vector3> { o0, o1, o2, o3, i0, i1, i2, i3 };

        // Simple UVs if you ever need them
        var uvs = new List<Vector2>
        {
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(0, 1),
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(0, 1),
        };

        var tris = new List<int>(24);
        // 4 quads (bottom, right, top, left), each as 2 triangles
        // CCW winding so normals face +Y

        // Bottom edge: o0 o1 i1 i0
        tris.AddRange(new int[] { 0, 1, 5, 0, 5, 4 });

        // Right edge: o1 o2 i2 i1
        tris.AddRange(new int[] { 1, 2, 6, 1, 6, 5 });

        // Top edge: o2 o3 i3 i2
        tris.AddRange(new int[] { 2, 3, 7, 2, 7, 6 });

        // Left edge: o3 o0 i0 i3
        tris.AddRange(new int[] { 3, 0, 4, 3, 4, 7 });

        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    Material makeMaterial(Color color)
    {
        Material material = new Material(baseMaterial);
        material.SetColor("_EmissionColor", color);
        materials[(Color32)color] = material;
        return material;
    }

    Material getMaterial(Color color)
    {
        Material material = materials[(Color32)color];
        if (material != null)
            return material;

        return makeMaterial(color);
    }

    void initDefaultMaterials()
    {
        // Allow GPU instancing of the material
        baseMaterial.enableInstancing = true;
        // Emission setup (Built-in/URP)
        baseMaterial.EnableKeyword("_EMISSION");
        // Don't contribute to GI (so bright rings don't bake/light the scene)
        baseMaterial.globalIlluminationFlags =
            MaterialGlobalIlluminationFlags.EmissiveIsBlack;

        foreach (Color color in Game.DEFAULT_COLORS)
            makeMaterial(color);
    }

    void Awake()
    {
        if (baseMaterial == null)
            Game.Quit(
                "EntitySelectionDisplay: missing required material, cannot render without."
            );

        if (!singleton)
            singleton = this;

        initDefaultMaterials();

        if (!ringMesh)
            ringMesh = makeRingOutlineMesh();

        if (!boxMesh)
            boxMesh = makeBoxOutlineMesh();
    }

    void drawMatrices(List<Matrix4x4> matrices, Mesh mesh, Material material)
    {
        Graphics.DrawMeshInstanced(
            mesh,
            0,
            material,
            matrices,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false
        );
    }

    public void DrawForEntities(List<Entity> entities) =>
        DrawForEntities(entities, Color.white, boxMesh);

    public void DrawForEntities(List<Entity> entities, Color color) =>
        DrawForEntities(entities, color, boxMesh);

    public void DrawForEntities(List<Entity> entities, Color color, Mesh mesh)
    {
        if (entities.Count == 0)
            return;

        matrices.Clear();
        Material material = getMaterial(color);

        for (int i = 0; i < entities.Count; i++)
        {
            Entity ent = entities[i];
            Vector3 scale = new Vector3(ent.halfSize, 1f, ent.halfSize);
            matrices.Add(
                Matrix4x4.TRS(ent.transform.position, rotation, scale)
            );

            if (matrices.Count == 1023)
            {
                drawMatrices(matrices, mesh, material);
                matrices.Clear();
            }
        }

        if (matrices.Count > 0)
        {
            drawMatrices(matrices, mesh, material);
            matrices.Clear();
        }
    }
}
