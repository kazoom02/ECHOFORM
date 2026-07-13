using UnityEngine;

// =====================================================
// ECHOFORM — PlayTimeTracker
// Accumulates total time played across a run, surviving scene
// loads. Read TotalSeconds when you save:
//
//     SaveSystem.Save(new SaveData {
//         slotName    = "Fight 2",
//         playSeconds = PlayTimeTracker.Instance.TotalSeconds,
//     });
//
// - New Game        -> PlayTimeTracker.Instance.BeginNewRun();
// - Load a save     -> PlayTimeTracker.Instance.ResumeFrom(data.playSeconds);
// - Pause menu open -> Pause();  close -> Resume();
//
// It uses UNSCALED time, so combat hitstop (Time.timeScale = 0)
// doesn't stop the clock — only an explicit Pause() does.
// Drop one in your first-loaded scene (or a bootstrap scene);
// it persists itself from there.
// =====================================================

public class PlayTimeTracker : MonoBehaviour
{
    public static PlayTimeTracker Instance { get; private set; }

    [SerializeField] private bool countOnStart = false;

    public float TotalSeconds { get; private set; }
    public bool IsCounting { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static PlayTimeTracker EnsureInstance()
    {
        if (Instance != null) return Instance;

        PlayTimeTracker existing = FindAnyObjectByType<PlayTimeTracker>();
        if (existing != null) return existing;

        GameObject trackerObject = new GameObject("PlayTimeTracker");
        return trackerObject.AddComponent<PlayTimeTracker>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        IsCounting = countOnStart;
    }

    void Update()
    {
        if (IsCounting) TotalSeconds += Time.unscaledDeltaTime;
    }

    /// <summary>Fresh run from zero (call on New Game).</summary>
    public void BeginNewRun()
    {
        TotalSeconds = 0f;
        IsCounting = true;
    }

    /// <summary>Continue a loaded save's accumulated time (call after loading).</summary>
    public void ResumeFrom(float seconds)
    {
        TotalSeconds = Mathf.Max(0f, seconds);
        IsCounting = true;
    }

    public void Pause() => IsCounting = false;
    public void Resume() => IsCounting = true;
}
