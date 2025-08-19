using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GameMode
{
    SinglePlayer,
    MultiPlayer,
}

/// <summary>
///     The Layers defined in the editor, used as masks for ray casting.
///
///     Can use bitwise OR to select multiple layers at once:
///         ex: Layers.EnvironmentMask | Layers.EntityMask
/// </summary>
public static class Layers
{
    // UNITY DEFAULT LAYERS
    // hardcoded for your pleasure, since (they cannot change!).
    public const int Default = 1 << 0;
    public const int TransparentFX = 1 << 1;
    public const int IgnoreRaycast = 1 << 2;
    public const int Water = 1 << 4;
    public const int UI = 1 << 5;

    public static readonly int Environment;
    public static readonly int Entity;
    public static readonly int Blurp;

    /// <summary>
    ///     Throws and ends game when the layer name is not found!
    ///
    ///     Used for failing fast during development, making the game
    ///     not start up when a layer we define here does not actually exist.
    /// </summary>
    ///
    static int MaskOrThrow(string name)
    {
        int layer = LayerMask.NameToLayer(name);
        if (layer != -1)
            return 1 << layer;

        throw Game.Quit(
            $"Layer '{name}' not found in Project Settings > Tags and Layers"
        );
    }

    static Layers()
    {
        Environment = MaskOrThrow("Environment");
        Entity = MaskOrThrow("Entity");
    }
}

public class Game : MonoBehaviour
{
    public static Game singleton { get; private set; }

    // Settings for the game mode of our current instance.
    private GameMode gameMode = GameMode.SinglePlayer;
    public bool isMulti => gameMode == GameMode.MultiPlayer;

    // // Settings for the camera in our current instance.
    // private Camera mainCamera;
    // public Camera MainCamera => mainCamera??= Camera.main;

    // Variables for stopping the game automatically.
    private DateTime start;
    public int runtimeInMinutes = 1;
    private TimeSpan maximumRuntime;

    /// <summary> Set controller to be a singleton, and capture starting time. </summary>
    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        DontDestroyOnLoad(gameObject);

        setStartAndEndTime();
        ensureLayers();
    }

    void setStartAndEndTime()
    {
        start = DateTime.Now;
        maximumRuntime = new TimeSpan(
            hours: 0,
            minutes: runtimeInMinutes,
            seconds: 0
        );
    }

    /// <summary> Access custom layer to ensure Layers class instantiates. </summary>
    void ensureLayers()
    {
        if (Layers.Environment < 0)
            Quit("Layers did not instantiate");
    }

    void FixedUpdate()
    {
        ExitWhenRuntimeExceeded();
    }

    /// <summary> Stop the game, when we have exceeded the configured runtime. </summary>
    void ExitWhenRuntimeExceeded()
    {
        if (DateTime.Now - start > maximumRuntime)
        {
            Quit(
                $"Maximum runtime reached. Exiting the game. runtime: {maximumRuntime}"
            );
        }
    }

    public static Maybe<Vector3> GetHit()
    {
        return GetHit(Input.mousePosition);
    }

    public static Maybe<Vector3> GetHit(Vector3 mousePosition)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out hit, 100f, Layers.Environment))
        {
            if (hit.transform != null)
            {
                return Maybe<Vector3>.of(hit.point);
            }
        }
        return Maybe<Vector3>.Nothing;
    }

    public static Exception Quit(string message)
    {
        Debug.LogError(message);
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Stop play mode
#else
        Application.Quit(); // Quit app if built
#endif
        return new Exception(message);
    }
}
