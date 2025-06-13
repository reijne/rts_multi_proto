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

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);
        start = DateTime.Now;
        maximumRuntime = new TimeSpan(
            hours: 0,
            minutes: runtimeInMinutes,
            seconds: 0
        );
    }

    void FixedUpdate()
    {
        ExitWhenRuntimeExceeded();
    }

    /// <summary>
    /// When we are over the maximum runtime, stop the game.
    /// </summary>
    void ExitWhenRuntimeExceeded()
    {
        if (DateTime.Now - start > maximumRuntime)
        {
            Debug.LogError(
                "Maximum runtime reached. Exiting the game. runtime:"
                    + maximumRuntime
            );
#if UNITY_EDITOR
            EditorApplication.isPlaying = false; // Stop play mode
#else
            Application.Quit(); // Quit app if built
#endif
        }
    }
}
