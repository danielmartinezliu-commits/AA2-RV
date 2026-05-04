using UnityEngine;

/// <summary>
/// Emite gotas de líquido desde el pitorro de la botella en función de:
///   · El ángulo de inclinación (cuanto más inclinada, más caudal)
///   · El nivel de llenado    (presión hidrostática — más llena, más rápido)
///
/// Al emitir, reduce gradualmente el <see cref="LiquidController.fillAmount"/>.
/// Expone <see cref="CurrentPourStrength"/> para que LiquidCascade pueda
/// usarlo cuando se active la cascada en el futuro.
/// </summary>
public class LiquidPourer : MonoBehaviour
{
    // ── Referencias ───────────────────────────────────────────────────────────

    [Header("Referencias")]
    [SerializeField] LiquidController  liquidController;
    [SerializeField] LiquidDropletPool pool;
    [Tooltip("Transform vacío situado en la boca/pitorro de la botella.")]
    [SerializeField] Transform         spout;

    // ── Ángulo de vertido ─────────────────────────────────────────────────────

    [Header("Ángulo de vertido")]
    [Tooltip("La botella empieza a verter a partir de este ángulo (0 = vertical).")]
    [Range(50f, 150f)]
    [SerializeField] float pourAngleStart = 80f;

    [Tooltip("A este ángulo se alcanza el caudal base máximo.")]
    [Range(90f, 175f)]
    [SerializeField] float pourAngleFull  = 140f;

    [Tooltip("Boost adicional entre pourAngleFull y 180°.\n2 = a 180° el caudal es 3× el máximo normal.")]
    [Range(0f, 5f)]
    [SerializeField] float invertBoost    = 2f;

    [Tooltip("Curva del boost de inversión. 2 = arranque suave, explosión al final.")]
    [Range(0.5f, 4f)]
    [SerializeField] float invertCurveExp = 2f;

    [Tooltip("A partir de este ángulo el nivel de llenado deja de frenar el vertido.")]
    [Range(140f, 179f)]
    [SerializeField] float fillIgnoreAngle = 170f;

    // ── Caudal ────────────────────────────────────────────────────────────────

    [Header("Caudal")]
    [Tooltip("Exponente de presión del líquido.\n1 = lineal  2 = cuadrático (recomendado)  3 = cúbico.")]
    [Range(0.5f, 4f)]
    [SerializeField] float fillPressureExp = 2f;

    [Tooltip("Gotas por segundo al caudal máximo.")]
    [Range(1f, 200f)]
    [SerializeField] float maxDropRate    = 120f;

    [Tooltip("Velocidad inicial de cada gota (m/s).")]
    [Range(0.1f, 3f)]
    [SerializeField] float dropSpeed      = 0.8f;

    [Tooltip("Dispersión aleatoria del chorro.")]
    [Range(0f, 0.5f)]
    [SerializeField] float dropSpread     = 0.08f;

    [Tooltip("Tiempo de vida de cada gota (s).")]
    [Range(0.5f, 5f)]
    [SerializeField] float dropLifetime   = 2.5f;

    // ── Vaciado ───────────────────────────────────────────────────────────────

    [Header("Vaciado")]
    [Tooltip("Fracción de llenado que se pierde por segundo al caudal máximo.")]
    [Range(0.01f, 0.5f)]
    [SerializeField] float drainRate      = 0.06f;

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Fuerza combinada del vertido [0 … ~3+].
    /// 0 = no vierte. Valores >1 ocurren cuando la botella está casi boca abajo.
    /// LiquidCascade lo usa para escalar el chorro visual (disponible a futuro).
    /// </summary>
    public float CurrentPourStrength { get; private set; }

    // ── Estado interno ────────────────────────────────────────────────────────

    float _emitTimer;

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!ValidateReferences())
        {
            CurrentPourStrength = 0f;
            return;
        }

        float angle = Vector3.Angle(transform.up, Vector3.up);
        float pourT = ComputePourFraction(angle);
        float fill  = liquidController.fillAmount;

        if (pourT <= 0f || fill <= 0f)
        {
            CurrentPourStrength = 0f;
            _emitTimer = 0f;
            return;
        }

        // Presión del líquido: cuanto más lleno, más fuerza.
        // Cerca de 180° la gravedad domina → Lerp suave hacia 1 (fill no frena).
        float fillPressure  = Mathf.Pow(fill, fillPressureExp);
        float gravityT      = Mathf.InverseLerp(fillIgnoreAngle, 180f, angle);
        CurrentPourStrength = pourT * Mathf.Lerp(fillPressure, 1f, gravityT);

        // Drenar líquido
        liquidController.AddFill(-CurrentPourStrength * drainRate * Time.deltaTime);

        // Emitir gotas
        float rate     = CurrentPourStrength * maxDropRate;
        float interval = 1f / rate;
        _emitTimer += Time.deltaTime;

        while (_emitTimer >= interval)
        {
            _emitTimer -= interval;
            EmitDrop();
        }
    }

    // ── Lógica interna ────────────────────────────────────────────────────────

    /// <summary>
    /// Etapa 1 [pourAngleStart → pourAngleFull]: 0 → 1  (rampa lineal)
    /// Etapa 2 [pourAngleFull  → 180°         ]: +0 → +invertBoost (boost de inversión)
    /// </summary>
    float ComputePourFraction(float angle)
    {
        float tilt       = Mathf.InverseLerp(pourAngleStart, pourAngleFull, angle);
        float invertT    = Mathf.InverseLerp(pourAngleFull, 180f, angle);
        float boostValue = Mathf.Pow(invertT, invertCurveExp) * invertBoost;
        return tilt + boostValue;
    }

    void EmitDrop()
    {
        LiquidDroplet drop = pool.Get();
        if (drop == null) return;

        Vector3 pos = spout.position + Random.insideUnitSphere * (dropSpread * 0.3f);
        Vector3 vel = Vector3.down * (dropSpeed * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(CurrentPourStrength)))
                    + Random.insideUnitSphere * dropSpread;

        drop.Spawn(pool, pos, vel, dropLifetime);
    }

    bool ValidateReferences()
    {
        if (liquidController == null || pool == null || spout == null)
        {
            Debug.LogWarning("[LiquidPourer] Faltan referencias. Revisa el Inspector.", this);
            return false;
        }
        return true;
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (spout == null) return;

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireSphere(spout.position, 0.015f);
        Gizmos.DrawRay(spout.position, Vector3.down * 0.15f);

        float angle = Vector3.Angle(transform.up, Vector3.up);
        float pourT = Application.isPlaying ? ComputePourFraction(angle) : 0f;
        Gizmos.color = Color.Lerp(Color.yellow, Color.red, pourT);
        Gizmos.DrawRay(transform.position, transform.up * 0.2f);
    }
}
