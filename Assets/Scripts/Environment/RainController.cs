using UnityEngine;

// =====================================================
// ECHOFORM — RainController
// Configures and drives a 2D rain Particle System so you
// don't have to hand-tune 20 particle fields. Drops fall as
// stretched streaks over a box area; intensity (0..1) scales
// the emission and is read by WaterAccumulator to fill the
// ground. Optionally sprays splash particles at the water
// surface.
//
// SETUP:
//   1. Create an empty GameObject "Rain", place it above the
//      arena (its X centre = arena centre).
//   2. Add a Particle System to it, then add this component
//      (it auto-configures the system on Awake).
//   3. (Optional) make a second small Particle System for
//      splashes and drag it into "Splash System".
// Tune the fields, then right-click the component ▸
// "Apply Rain Settings" to preview in the editor.
// =====================================================

[RequireComponent(typeof(ParticleSystem))]
public class RainController : MonoBehaviour
{
    [Header("Rain area (relative to this object)")]
    [SerializeField] private float width = 30f;
    [SerializeField] private float spawnHeight = 12f;     // how far above this object drops appear

    [Header("Fall")]
    [SerializeField] private float fallSpeed = 22f;
    [SerializeField] private float wind = -3f;             // horizontal drift
    [SerializeField, Range(0f, 1f)] private float intensity = 0.6f;
    [SerializeField] private float maxEmission = 600f;     // drops/sec at intensity 1

    [Header("Look")]
    [SerializeField] private Color rainColor = new Color(0.6f, 0.85f, 1f, 0.5f);
    [SerializeField] private float dropStretch = 2.2f;     // streak length
    [SerializeField] private float dropSize = 0.06f;

    [Header("Splashes (optional)")]
    [SerializeField] private ParticleSystem splashSystem;
    [SerializeField] private float splashesPerSecond = 40f;

    // World Y where splashes spawn — WaterAccumulator raises this as water pools.
    public float SurfaceY { get; set; }

    public float Intensity => isActiveAndEnabled ? intensity : 0f;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private float splashCarry;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
        SurfaceY = transform.position.y - spawnHeight;   // sensible default = below the emitter
        Configure();

        if (psr.sharedMaterial == null)
            psr.material = new Material(Shader.Find("Sprites/Default"));
    }

    /// <summary>Set rain strength at runtime (0 = clear, 1 = downpour).</summary>
    public void SetIntensity(float t)
    {
        intensity = Mathf.Clamp01(t);
        if (ps == null) ps = GetComponent<ParticleSystem>();
        var em = ps.emission;
        em.rateOverTime = intensity * maxEmission;
    }

    private void Update()
    {
        if (splashSystem == null || intensity <= 0.01f) return;

        // Emit splash puffs along the surface, scaled by rain intensity.
        splashCarry += splashesPerSecond * intensity * Time.deltaTime;
        int n = Mathf.FloorToInt(splashCarry);
        splashCarry -= n;

        for (int i = 0; i < n; i++)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = new Vector3(
                    transform.position.x + Random.Range(-width * 0.5f, width * 0.5f),
                    SurfaceY,
                    0f)
            };
            splashSystem.Emit(ep, 1);
        }
    }

    [ContextMenu("Apply Rain Settings")]
    private void Configure()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (psr == null) psr = GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSpeed = 0f;                              // motion comes from velocityOverLifetime
        main.startSize = dropSize;
        main.startColor = rainColor;
        main.startLifetime = (spawnHeight + 6f) / Mathf.Max(1f, fallSpeed);
        main.gravityModifier = 0f;
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = intensity * maxEmission;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.1f, 1f);
        shape.position = new Vector3(0f, spawnHeight, 0f);
        shape.rotation = Vector3.zero;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(wind);
        vel.y = new ParticleSystem.MinMaxCurve(-fallSpeed);

        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.lengthScale = dropStretch;
        psr.velocityScale = 0.08f;
    }
}
