using UnityEngine;

/// <summary>
/// Drives the FakeLiquid shader on this GameObject's Renderer.
///
/// Drop this on the liquid-mesh object (the interior fill mesh).
/// The shader clips geometry above a horizontal plane in world space,
/// so the liquid surface stays flat regardless of how you tilt the bottle.
/// </summary>
[RequireComponent(typeof(Renderer), typeof(MeshFilter))]
public class LiquidController : MonoBehaviour
{
    // ── Public API ───────────────────────────────────────────────────────────

    [Header("Fill")]
    [Range(0f, 1f)]
    public float fillAmount = 0.5f;

    [Header("Wobble decay (higher = faster stop)")]
    [Range(0.1f, 10f)]
    public float wobbleDecay = 2f;
    [Range(0f, 1f)]
    public float maxWobble   = 0.5f;

    [Header("Mesh bounds (auto-filled from MeshFilter)")]
    public float localBoundMin = -0.5f;
    public float localBoundMax =  0.5f;

    // ── Shader property IDs ──────────────────────────────────────────────────

    static readonly int ID_FillAmount    = Shader.PropertyToID("_FillAmount");
    static readonly int ID_LocalBoundMin = Shader.PropertyToID("_LocalBoundMin");
    static readonly int ID_LocalBoundMax = Shader.PropertyToID("_LocalBoundMax");
    static readonly int ID_WobbleX       = Shader.PropertyToID("_WobbleX");
    static readonly int ID_WobbleZ       = Shader.PropertyToID("_WobbleZ");

    // ── Private state ────────────────────────────────────────────────────────

    Material   _mat;
    Quaternion _prevRot;
    float      _wobbleX;
    float      _wobbleZ;

    // ── Unity callbacks ──────────────────────────────────────────────────────

    void Awake()
    {
        _mat = GetComponent<Renderer>().material;   // instance copy

        // Auto-detect bounds from mesh
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh != null)
        {
            localBoundMin = mesh.bounds.min.y;
            localBoundMax = mesh.bounds.max.y;
        }

        _mat.SetFloat(ID_LocalBoundMin, localBoundMin);
        _mat.SetFloat(ID_LocalBoundMax, localBoundMax);

        _prevRot = transform.rotation;
    }

    void Update()
    {
        UpdateWobble();
        PushToShader();
        _prevRot = transform.rotation;
    }

    // ── Fill control (callable from outside, e.g. pouring logic) ────────────

    public void SetFill(float amount) => fillAmount = Mathf.Clamp01(amount);

    public void AddFill(float delta)  => fillAmount = Mathf.Clamp01(fillAmount + delta);

    // ── Internal ─────────────────────────────────────────────────────────────

    void UpdateWobble()
    {
        // Angular velocity from rotation delta
        Quaternion delta = transform.rotation * Quaternion.Inverse(_prevRot);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);

        // Wrap angle to [-180, 180]
        if (angleDeg > 180f) angleDeg -= 360f;

        float angVel = angleDeg / Mathf.Max(Time.deltaTime, 1e-4f);

        // Rotation around world Z → wobble on X; rotation around world X → wobble on Z
        _wobbleX += Mathf.Clamp(axis.z * angVel * 0.0005f, -maxWobble, maxWobble);
        _wobbleZ += Mathf.Clamp(axis.x * angVel * 0.0005f, -maxWobble, maxWobble);

        _wobbleX = Mathf.Clamp(_wobbleX, -maxWobble, maxWobble);
        _wobbleZ = Mathf.Clamp(_wobbleZ, -maxWobble, maxWobble);

        // Exponential decay
        float decay = Mathf.Exp(-wobbleDecay * Time.deltaTime);
        _wobbleX *= decay;
        _wobbleZ *= decay;
    }

    void PushToShader()
    {
        _mat.SetFloat(ID_FillAmount, fillAmount);
        _mat.SetFloat(ID_WobbleX,    _wobbleX);
        _mat.SetFloat(ID_WobbleZ,    _wobbleZ);
    }

    // ── Editor live-preview ───────────────────────────────────────────────────

    void OnValidate()
    {
        if (!Application.isPlaying || _mat == null) return;
        _mat.SetFloat(ID_FillAmount,    fillAmount);
        _mat.SetFloat(ID_LocalBoundMin, localBoundMin);
        _mat.SetFloat(ID_LocalBoundMax, localBoundMax);
    }

    void OnDestroy()
    {
        // Avoid material leak (we created an instance copy)
        if (_mat != null) Destroy(_mat);
    }
}
