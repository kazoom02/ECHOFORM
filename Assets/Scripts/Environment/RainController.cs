using UnityEngine;
using UnityEngine.Audio;

// =====================================================
// ECHOFORM — RainController
// Cria e configura em tempo de execução um sistema de partículas de chuva
// com intensidade, vento, aspeto e som ambiente ajustáveis.
// =====================================================

[DisallowMultipleComponent]
public class RainController : MonoBehaviour
{
    [Header("Rain area (relative to this object)")]
    [SerializeField] private float width = 20f;
    [SerializeField] private float spawnHeight = 11.5f;

    [Header("Fall")]
    [SerializeField] private float fallSpeed = 14f;
    [SerializeField] private float wind = -2f;
    [SerializeField, Range(0f, 1f)] private float intensity = 0.7f;
    [SerializeField] private float maxEmission = 520f;

    [Header("Look")]
    [SerializeField] private Color rainColor = new(0.55f, 0.78f, 1f, 0.34f);
    [SerializeField] private float dropStretch = 3f;
    [SerializeField] private float dropSize = 0.045f;
    [SerializeField] private string sortingLayerName = "Characters";
    [SerializeField] private int sortingOrder = 10;

    [Header("Rain ambience (optional)")]
    [SerializeField] private AudioClip rainLoop;
    [SerializeField] private AudioMixerGroup outputGroup;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.45f;

    [Header("Splashes (optional)")]
    [SerializeField] private ParticleSystem splashSystem;
    [SerializeField] private float splashesPerSecond = 40f;

        public float SurfaceY { get; set; }

    public float Intensity => isActiveAndEnabled ? intensity : 0f;

    private ParticleSystem rainSystem;
    private ParticleSystemRenderer rainRenderer;
    private AudioSource ambienceSource;
    private Material runtimeMaterial;
    private float splashCarry;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!initialized)
            Initialize();

        if (rainSystem != null && intensity > 0.001f && !rainSystem.isPlaying)
            rainSystem.Play();

        if (ambienceSource != null && rainLoop != null && !ambienceSource.isPlaying)
            ambienceSource.Play();
    }

    private void OnDisable()
    {
        if (rainSystem != null)
            rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (ambienceSource != null)
            ambienceSource.Stop();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

        public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);

        if (!initialized)
            Initialize();

        var emission = rainSystem.emission;
        emission.rateOverTime = intensity * maxEmission;

        if (ambienceSource != null)
            ambienceSource.volume = ambientVolume * intensity;

        if (intensity > 0.001f)
        {
            if (!rainSystem.isPlaying)
                rainSystem.Play();
        }
        else
        {
            rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void Update()
    {
        if (splashSystem == null || intensity <= 0.01f)
            return;

        splashCarry += splashesPerSecond * intensity * Time.deltaTime;
        int splashCount = Mathf.FloorToInt(splashCarry);
        splashCarry -= splashCount;

        for (int i = 0; i < splashCount; i++)
        {
            var emitParams = new ParticleSystem.EmitParams
            {
                position = new Vector3(
                    transform.position.x + Random.Range(-width * 0.5f, width * 0.5f),
                    SurfaceY,
                    transform.position.z)
            };

            splashSystem.Emit(emitParams, 1);
        }
    }

    [ContextMenu("Apply Rain Settings")]
    private void ApplyRainSettings()
    {
        EnsureParticleSystem();
        ConfigureParticleSystem();
    }

    private void Initialize()
    {
        EnsureParticleSystem();
        ConfigureParticleSystem();
        ConfigureAmbience();
        SurfaceY = transform.position.y;
        initialized = true;
    }

    private void EnsureParticleSystem()
    {
        rainSystem = GetComponent<ParticleSystem>();
        if (rainSystem == null)
            rainSystem = gameObject.AddComponent<ParticleSystem>();

        rainRenderer = GetComponent<ParticleSystemRenderer>();
    }

    private void ConfigureParticleSystem()
    {
        bool restartAfterConfiguration = rainSystem.isPlaying;
        rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = rainSystem.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startSize = dropSize;
        main.startColor = rainColor;
        main.startLifetime = (spawnHeight + 1.5f) / Mathf.Max(1f, fallSpeed);
        main.gravityModifier = 0f;
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = rainSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = intensity * maxEmission;

        var shape = rainSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.1f, 0.1f);
        shape.position = new Vector3(0f, spawnHeight, 0f);
        shape.rotation = Vector3.zero;

        var velocity = rainSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        velocity.x = new ParticleSystem.MinMaxCurve(wind);
        velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        var colorOverLifetime = rainSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.9f, 0.88f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        if (rainRenderer != null)
        {
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.lengthScale = dropStretch;
            rainRenderer.velocityScale = 0.08f;
            rainRenderer.sortingLayerName = sortingLayerName;
            rainRenderer.sortingOrder = sortingOrder;
            EnsureRainMaterial();
        }

        if (restartAfterConfiguration)
            rainSystem.Play();
    }

    private void EnsureRainMaterial()
    {
        if (runtimeMaterial != null)
        {
            rainRenderer.sharedMaterial = runtimeMaterial;
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        runtimeMaterial = new Material(shader)
        {
            name = "Area 2 Rain (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        rainRenderer.sharedMaterial = runtimeMaterial;
    }

    private void ConfigureAmbience()
    {
        if (rainLoop == null)
            return;

        ambienceSource = GetComponent<AudioSource>();
        if (ambienceSource == null)
            ambienceSource = gameObject.AddComponent<AudioSource>();

        ambienceSource.clip = rainLoop;
        ambienceSource.outputAudioMixerGroup = outputGroup;
        ambienceSource.loop = true;
        ambienceSource.playOnAwake = false;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.volume = ambientVolume * intensity;
    }
}
