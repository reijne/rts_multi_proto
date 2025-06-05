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

    public GameMode gameMode { get; private set; } = GameMode.SinglePlayer;
    private DateTime start;
    public int runtimeInMinutes = 1;
    private TimeSpan maximumRuntime;

    public bool isMulti => gameMode == GameMode.MultiPlayer;

    void Start()
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
            seconds: 10
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
