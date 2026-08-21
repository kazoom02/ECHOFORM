using UnityEngine;

// =====================================================
// ECHOFORM — WaterAccumulator
// Simula a acumulação e o escoamento de água em função da chuva e pode
// controlar um reflexo simples da personagem na superfície.
// =====================================================

public class WaterAccumulator : MonoBehaviour
{
    [Header("Water sprite (SpriteRenderer, Tiled/Sliced, pivot = Bottom)")]
    [SerializeField] private SpriteRenderer water;
    [SerializeField] private float groundY = -4f;
    [SerializeField] private float width = 30f;

    [Header("Accumulation")]
    [SerializeField] private float maxDepth = 2f;
    [SerializeField] private float riseSpeed = 0.15f;
    [SerializeField] private float evaporateSpeed = 0.05f;
    [SerializeField, Range(0f, 4f)] private float startDepth = 0f;

    [Header("Links (optional)")]
    [SerializeField] private RainController rain;
    [SerializeField] private SpriteRenderer reflection;
    [SerializeField, Range(0f, 1f)] private float reflectionMaxAlpha = 0.35f;
    [SerializeField] private float reflectionWobble = 0.04f;
    [SerializeField] private float reflectionWobbleSpeed = 2f;

    public float Depth { get; private set; }
    public float SurfaceY => groundY + Depth;

    private float reflectionBaseX;

    private void Start()
    {
        Depth = Mathf.Clamp(startDepth, 0f, maxDepth);
        if (reflection != null) reflectionBaseX = reflection.transform.localPosition.x;
        Apply();
    }

    private void Update()
    {
        float rainAmt = rain != null ? rain.Intensity : 0f;

        if (rainAmt > 0.01f) Depth += riseSpeed * rainAmt * Time.deltaTime;
        else                 Depth -= evaporateSpeed * Time.deltaTime;

        Depth = Mathf.Clamp(Depth, 0f, maxDepth);
        Apply();
    }

    private void Apply()
    {

        if (water != null)
        {
            water.size = new Vector2(width, Mathf.Max(0.0001f, Depth));
            var p = water.transform.position;
            water.transform.position = new Vector3(p.x, groundY, p.z);
        }

        if (rain != null) rain.SurfaceY = SurfaceY;

        if (reflection != null)
        {
            float a = Mathf.InverseLerp(0f, maxDepth, Depth) * reflectionMaxAlpha;
            var c = reflection.color; c.a = a; reflection.color = c;

            float wob = Mathf.Sin(Time.time * reflectionWobbleSpeed) * reflectionWobble;
            var lp = reflection.transform.localPosition;
            reflection.transform.localPosition = new Vector3(reflectionBaseX + wob, lp.y, lp.z);
        }
    }
}
