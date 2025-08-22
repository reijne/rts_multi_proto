using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HealthBarDisplay : MonoBehaviour
{
    public static HealthBarDisplay singleton;

    [Header("Materials (Unlit, Instancing On)")]
    public Material backgroundMaterial;
    public Material fillMaterial;

    [Header("Sizing")]
    readonly float height = 0.25f;

    static Mesh quad;
    readonly List<Entity> tracked = new();
    readonly List<Matrix4x4> bgMatrices = new(1023);
    readonly List<Matrix4x4> fillMatrices = new(1023);

    Camera cam;

    public void Add(Entity ent)
    {
        if (ent.health != null)
            tracked.Add(ent);
    }

    public void Remove(Entity ent)
    {
        tracked.Remove(ent);
    }

    void Awake()
    {
        if (!backgroundMaterial)
            Game.Quit("HealthBarDisplay: missing required background material");

        if (!fillMaterial)
            Game.Quit("HealthBarDisplay: missing required fill material");

        if (!singleton)
            singleton = this;

        if (!quad)
            quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        cam = Camera.main;

        backgroundMaterial.enableInstancing = true;
        fillMaterial.enableInstancing = true;
    }

    void LateUpdate()
    {
        if (tracked.Count == 0)
            return;

        // TODO: Remove this shit, just use forward always.
        var forward = cam ? cam.transform.forward : Vector3.forward;
        var rot = Quaternion.LookRotation(forward, Vector3.up);
        var right = rot * Vector3.right;

        bgMatrices.Clear();
        fillMatrices.Clear();

        for (int i = 0; i < tracked.Count; i++)
        {
            var ent = tracked[i];
            if (!ent.IsEnabled)
                continue;

            // Width/height in world units
            float width = ent.halfSize;

            // Health ratio 0..1
            float t = Mathf.Clamp01(
                ent.health.currentHealth / ent.health.healthData.Health
            );

            Vector3 basePos =
                ent.transform.position + new Vector3(0, ent.height + height, 0);

            // Background (full width)
            bgMatrices.Add(
                Matrix4x4.TRS(basePos, rot, new Vector3(width, height, 1f))
            );
            if (bgMatrices.Count == 1023)
            {
                Draw(bgMatrices, backgroundMaterial);
                bgMatrices.Clear();
            }

            // Fill: left-anchored → offset by ((t-1)/2 * width) along local right
            float fillWidth = width * t;
            Vector3 fillPos = basePos + right * ((t - 1f) * 0.5f * width);
            fillMatrices.Add(
                Matrix4x4.TRS(fillPos, rot, new Vector3(fillWidth, height, 1f))
            );
            if (fillMatrices.Count == 1023)
            {
                Draw(fillMatrices, fillMaterial);
                fillMatrices.Clear();
            }
        }

        if (bgMatrices.Count > 0)
            Draw(bgMatrices, backgroundMaterial);
        if (fillMatrices.Count > 0)
            Draw(fillMatrices, fillMaterial);
    }

    void Draw(List<Matrix4x4> matrices, Material mat)
    {
        Graphics.DrawMeshInstanced(
            quad,
            0,
            mat,
            matrices,
            null,
            ShadowCastingMode.Off,
            false
        );
    }
}
