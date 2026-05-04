using UnityEngine;

/// <summary>
/// Una gota individual de líquido sacada del pool.
///
/// Setup del prefab:
///   MeshFilter   → Quad (primitiva de Unity)
///   MeshRenderer → material Custom/LiquidDroplet
///   Rigidbody    → auto-configurado en Awake
///   SphereCollider → radio pequeño (~0.02) para colisión con el vaso
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class LiquidDroplet : MonoBehaviour
{
    // ── Config ─────────────────────────────────────────────────────────────────
    [Header("Visual")]
    [Tooltip("Escala del quad en world units.")]
    public float dropSize = 0.1f;

    // ── State ──────────────────────────────────────────────────────────────────
    LiquidDropletPool _pool;
    Rigidbody         _rb;
    float             _lifetime;
    float             _elapsed;

    // ── Cache ──────────────────────────────────────────────────────────────────
    Transform _camTransform;

    // ── Unity callbacks ────────────────────────────────────────────────────────

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.mass                = 0.005f;
        _rb.linearDamping       = 0.1f;
        _rb.angularDamping      = 0f;
        _rb.interpolation       = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Congelamos rotación física: el billboard se encarga de la orientación
        _rb.constraints         = RigidbodyConstraints.FreezeRotation;

        var col = GetComponent<SphereCollider>();
        col.radius = dropSize * 0.5f;

        transform.localScale = Vector3.one * dropSize;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _lifetime)
            Release();
    }

    void LateUpdate()
    {
        // Billboard: la cara del quad siempre mira a la cámara
        if (_camTransform == null)
            _camTransform = Camera.main?.transform;

        if (_camTransform != null)
            transform.rotation = _camTransform.rotation;
    }

    // ── API pública ────────────────────────────────────────────────────────────

    /// <summary>Activa la gota desde el pool con posición, velocidad y tiempo de vida.</summary>
    public void Spawn(LiquidDropletPool pool, Vector3 position, Vector3 velocity, float lifetime)
    {
        _pool     = pool;
        _elapsed  = 0f;
        _lifetime = lifetime;

        transform.position  = position;
        _rb.linearVelocity  = velocity;
        _rb.angularVelocity = Vector3.zero;

        gameObject.SetActive(true);
    }

    /// <summary>Devuelve la gota al pool (llamada automáticamente al expirar o por OnCollisionEnter).</summary>
    public void Release()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        _pool?.Return(this);
    }

    void OnCollisionEnter(Collision col)
    {
        // La gota queda "pegada" un momento al contacto y luego se devuelve.
        // Puedes sustituir esto por un efecto de splash más adelante.
        _elapsed = _lifetime - 0.15f;  // la quita en 0.15 s tras el impacto
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;    // la fija en el punto de impacto
    }

    void OnDisable()
    {
        // Aseguramos que el Rigidbody no queda en modo kinematic para el siguiente uso
        if (_rb != null) _rb.isKinematic = false;
    }
}
