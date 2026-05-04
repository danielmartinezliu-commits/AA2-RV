using UnityEngine;

/// <summary>
/// Dibuja el chorro de líquido como un LineRenderer con trayectoria parabólica.
///
/// Setup:
///   1. Añade este script + LineRenderer al GameObject raíz de la botella.
///   2. Asigna pourer y spout.
///   3. Al LineRenderer asígnale un material con Custom/LiquidCascade.
///      · Texture Mode  → Stretch
///      · Alignment     → View  (siempre mira a la cámara, funciona bien en VR)
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LiquidCascade : MonoBehaviour
{
    // ── Referencias ───────────────────────────────────────────────────────────

    [Header("Referencias")]
    [SerializeField] LiquidPourer pourer;
    [Tooltip("El mismo Transform del pitorro que usa LiquidPourer.")]
    [SerializeField] Transform    spout;

    // ── Forma del chorro ──────────────────────────────────────────────────────

    [Header("Trayectoria")]
    [Tooltip("Número de puntos de la curva. 16 es suficiente para una parábola suave.")]
    [Range(6, 32)]
    [SerializeField] int   resolution      = 16;

    [Tooltip("Separación temporal entre puntos (s). Controla la longitud del arco simulado.")]
    [Range(0.02f, 0.12f)]
    [SerializeField] float stepTime        = 0.055f;

    [Tooltip("Velocidad inicial del chorro en la dirección del spout (m/s).")]
    [Range(0.2f, 4f)]
    [SerializeField] float initialSpeed    = 1.4f;

    [Tooltip("Distancia máxima antes de cortar el chorro si no hay colisión.")]
    [Range(0.5f, 6f)]
    [SerializeField] float maxFallDistance = 3.5f;

    [Tooltip("Capas que pueden detener el chorro (vaso, suelo…). Excluye la botella y el líquido.")]
    [SerializeField] LayerMask collisionMask = ~0;

    // ── Grosor ────────────────────────────────────────────────────────────────

    [Header("Grosor")]
    [Tooltip("Ancho del ribbon cuando la botella está completamente llena e invertida.")]
    [Range(0.003f, 0.06f)]
    [SerializeField] float widthMax  = 0.030f;

    [Tooltip("Ancho mínimo (casi vacía o recién empieza a verter).")]
    [Range(0.001f, 0.02f)]
    [SerializeField] float widthMin  = 0.006f;

    [Tooltip("El extremo del chorro se estrecha con este factor (simula aceleración por gravedad).")]
    [Range(0.2f, 1f)]
    [SerializeField] float endWidthRatio = 0.45f;

    // ── Ondulación ────────────────────────────────────────────────────────────

    [Header("Ondulación")]
    [Range(0f, 0.015f)]
    [SerializeField] float wobbleAmount = 0.004f;
    [Range(2f, 20f)]
    [SerializeField] float wobbleSpeed  = 9f;

    // ── Umbral de visibilidad ─────────────────────────────────────────────────

    [Tooltip("Fuerza mínima de vertido para mostrar el chorro.")]
    [Range(0f, 0.1f)]
    [SerializeField] float showThreshold = 0.02f;

    // ── Privado ───────────────────────────────────────────────────────────────

    LineRenderer _line;
    Vector3[]    _points;

    // ── Unity callbacks ───────────────────────────────────────────────────────

    void Awake()
    {
        _line  = GetComponent<LineRenderer>();
        _points = new Vector3[resolution];

        _line.useWorldSpace     = true;
        _line.positionCount     = resolution;
        _line.textureMode       = LineTextureMode.Stretch;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows    = false;
        // Vertex color del LineRenderer debe ser blanco opaco para no
        // multiplicar y oscurecer/transparentar el color del material.
        _line.startColor = Color.white;
        _line.endColor   = Color.white;
        _line.enabled    = false;
    }

    void LateUpdate()
    {
        if (pourer == null || spout == null)
        {
            _line.enabled = false;
            return;
        }

        float strength = pourer.CurrentPourStrength;

        if (strength < showThreshold)
        {
            _line.enabled = false;
            return;
        }

        _line.enabled = true;
        UpdateWidth(strength);
        BuildStream(strength);
    }

    // ── Lógica ────────────────────────────────────────────────────────────────

    void UpdateWidth(float strength)
    {
        float w          = Mathf.Lerp(widthMin, widthMax, Mathf.Clamp01(strength));
        _line.startWidth = w;
        _line.endWidth   = w * endWidthRatio;
    }

    void BuildStream(float strength)
    {
        Vector3 origin = spout.position;

        // Velocidad inicial: en la dirección del spout, escalada con la fuerza.
        // Mathf.Max garantiza un mínimo de impulso incluso al borde del umbral.
        Vector3 vel = spout.forward * (initialSpeed * Mathf.Max(strength, 0.15f));

        Vector3 gravity = Physics.gravity;
        float   t       = 0f;

        // ── 1. Genera todos los puntos de la parábola ─────────────────────────
        for (int i = 0; i < resolution; i++, t += stepTime)
        {
            Vector3 p = origin + vel * t + 0.5f * gravity * t * t;

            // Ondulación lateral sutil
            float w = Mathf.Sin(Time.time * wobbleSpeed + i * 1.8f) * wobbleAmount * strength;
            p.x += w;
            p.z += w * 0.6f;

            _points[i] = p;
        }

        // ── 2. Busca la primera colisión y trunca el chorro ahí ───────────────
        for (int i = 1; i < resolution; i++)
        {
            // Corte por distancia máxima
            if (Vector3.Distance(origin, _points[i]) >= maxFallDistance)
            {
                FillFrom(i);
                break;
            }

            // Raycast entre punto anterior y actual
            if (Physics.Linecast(_points[i - 1], _points[i], out RaycastHit hit, collisionMask))
            {
                _points[i] = hit.point;
                FillFrom(i + 1);
                break;
            }
        }

        _line.SetPositions(_points);
    }

    /// <summary>Repite el último punto válido hacia el final del array.</summary>
    void FillFrom(int startIndex)
    {
        if (startIndex >= resolution) return;
        Vector3 last = _points[startIndex - 1];
        for (int i = startIndex; i < resolution; i++)
            _points[i] = last;
    }
}
