using UnityEngine;

// =====================================================
// ECHOFORM — WaterAccumulator
// Raises a water sprite from the ground as it rains, and
// lets it slowly recede when the rain stops. Optionally
// drives a cheap reflection (a Y-flipped duplicate of the
// hero fading in on the water surface).
//
// This is a fake, not a fluid sim — perfect for a jam.
//
// SETUP:
//   1. Make the water a SpriteRenderer using a plain square
//      sprite. Set its Draw Mode to **Tiled** (or Sliced) and
//      its sprite pivot to **Bottom**, so growing the size
//      raises the surface from the ground up.
//   2. Put it on a sorting layer above the background, below
//      the characters. Position it at the ground line.
//   3. Drag it into "Water", set Ground Y to that line, and
//      (optional) link the RainController and a flipped
//      reflection SpriteRenderer.
// =====================================================

public class WaterAccumulator : MonoBehaviour
{
    [Header("Water sprite (SpriteRenderer, Tiled/Sliced, pivot = Bottom)")]
    [SerializeField] private SpriteRenderer water;
    [SerializeField] private float groundY = -4f;          // world Y of the ground line
    [SerializeField] private float width = 30f;

    [Header("Accumulation")]
    [SerializeField] private float maxDepth = 2f;          // deepest the pool can get
    [SerializeField] private float riseSpeed = 0.15f;      // units/sec at full rain
    [SerializeField] private float evaporateSpeed = 0.05f; // units/sec when dry
    [SerializeField, Range(0f, 4f)] private float startDepth = 0f;

    [Header("Links (optional)")]
    [SerializeField] private RainController rain;          // reads Intensity, feeds splash surface
    [SerializeField] private SpriteRenderer reflection;    // flipped duplicate of the hero
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
        // Grow the water surface upward from the ground line.
        if (water != null)
        {
            water.size = new Vector2(width, Mathf.Max(0.0001f, Depth));
            var p = water.transform.position;
            water.transform.position = new Vector3(p.x, groundY, p.z);   // pivot = Bottom keeps base on the ground
        }

        // Splashes land on the current surface.
        if (rain != null) rain.SurfaceY = SurfaceY;

        // Reflection fades in with depth and wobbles gently.
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
