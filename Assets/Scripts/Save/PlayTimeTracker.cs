using UnityEngine;

// =====================================================
// ECHOFORM — PlayTimeTracker
// Regista o tempo total de uma partida entre cenas, permitindo iniciar,
// retomar, suspender e continuar a contagem independentemente da escala temporal.
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

        public void BeginNewRun()
    {
        TotalSeconds = 0f;
        IsCounting = true;
    }

        public void ResumeFrom(float seconds)
    {
        TotalSeconds = Mathf.Max(0f, seconds);
        IsCounting = true;
    }

    public void Pause() => IsCounting = false;
    public void Resume() => IsCounting = true;
}
