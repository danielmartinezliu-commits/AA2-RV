using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script principal del vaso de bar para VR.
/// Acepta un único tipo de líquido, lleva la cuenta de unidades y las vierte al inclinar el vaso.
/// Requiere que el GameObject tenga un Rigidbody y un XRGrabInteractable.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class Glass : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Vertido")]
    [Tooltip("Ángulo de inclinación (en grados) a partir del cual el vaso vierte su contenido")]
    public float pourAngleThreshold = 100f;

    [Tooltip("Punto de spawn de las gotas vertidas (boca del vaso)")]
    public Transform pourPoint;

    [Tooltip("Prefab que se instancia al verter. Debe tener LiquidDroplet y ShakerInput.")]
    public GameObject outputPrefab;

    [Tooltip("Tiempo en segundos entre cada unidad vertida al inclinar el vaso")]
    public float timeBetweenPours = 0.1f;

    // ─────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────

    /// <summary>Tipo de líquido actualmente en el vaso (null si está vacío)</summary>
    private LiquidSO _currentLiquid;

    /// <summary>Cantidad de unidades del líquido dentro del vaso</summary>
    private int _liquidCount;

    /// <summary>Acumulador del timer de vertido entre unidades</summary>
    private float _timePouringWaited;

    private Rigidbody _rb;
    private XRGrabInteractable _grabInteractable;

    // ─────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void FixedUpdate()
    {
        HandlePouring();
    }

    // ─────────────────────────────────────────────────────────────
    //  INTRODUCCIÓN DE LÍQUIDOS (llamado desde GlassTopCollider)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Intenta añadir un líquido al vaso.
    /// Solo acepta el tipo de líquido que ya contiene; si está vacío acepta cualquiera.
    /// Si el tipo no coincide lo rechaza sin destruir el objeto.
    /// </summary>
    /// <param name="inputObject">GameObject con ShakerInput que entra por la boca del vaso</param>
    public void TryAddLiquid(GameObject inputObject)
    {
        ShakerInput shakerInput = inputObject.GetComponent<ShakerInput>();
        if (shakerInput == null)
        {
            Debug.Log("[Glass] El objeto no tiene ShakerInput, ignorando.");
            return;
        }

        if (shakerInput.liquidData == null)
        {
            Debug.LogWarning("[Glass] El ShakerInput no tiene LiquidSO asignado.");
            return;
        }

        LiquidSO incoming = shakerInput.liquidData;

        // Si el vaso está vacío, fijar el tipo de líquido que acepta
        if (_currentLiquid == null)
        {
            _currentLiquid = incoming;
            _liquidCount++;
            Debug.Log($"[Glass] Primer líquido añadido: {_currentLiquid.liquidName}. Unidades: {_liquidCount}");
            Destroy(inputObject);
            return;
        }

        // Si el tipo coincide con el que ya hay dentro, aceptar
        if (incoming == _currentLiquid)
        {
            _liquidCount++;
            Debug.Log($"[Glass] Líquido añadido: {_currentLiquid.liquidName}. Unidades: {_liquidCount}");
            Destroy(inputObject);
            return;
        }

        // Tipo diferente: rechazar sin destruir el objeto
        Debug.Log($"[Glass] Rechazado '{incoming.liquidName}': el vaso ya contiene '{_currentLiquid.liquidName}'.");
    }

    // ─────────────────────────────────────────────────────────────
    //  VERTIDO
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Comprueba si el vaso está suficientemente inclinado para verter.
    /// Vierte una unidad cada timeBetweenPours segundos mientras siga inclinado.
    /// </summary>
    private void HandlePouring()
    {
        // Sin contenido no hay nada que verter
        if (_currentLiquid == null || _liquidCount <= 0)
            return;

        float angle = Vector3.Angle(transform.up, Vector3.up);
        _timePouringWaited += Time.fixedDeltaTime;

        if (angle >= pourAngleThreshold && _timePouringWaited >= timeBetweenPours)
        {
            _timePouringWaited = 0f;
            PourUnit();
        }
    }

    /// <summary>
    /// Vierte una unidad del líquido: instancia una gota y decrementa el contador.
    /// Cuando el contador llega a 0 limpia el tipo de líquido, dejando el vaso vacío.
    /// </summary>
    private void PourUnit()
    {
        SpawnDroplet();

        _liquidCount--;
        Debug.Log($"[Glass] Vertida 1 unidad de '{_currentLiquid.liquidName}'. Restantes: {_liquidCount}");

        // Cuando se vacía completamente, liberar el tipo de líquido para aceptar uno nuevo
        if (_liquidCount <= 0)
        {
            Debug.Log($"[Glass] Vaso vaciado. Listo para recibir un nuevo tipo de líquido.");
            _currentLiquid = null;
            _liquidCount = 0;
        }
    }

    /// <summary>
    /// Instancia el prefab de salida, le asigna el LiquidSO actual y lanza la gota.
    /// Sigue el mismo patrón que el Shaker usando LiquidDroplet.SpawnBien.
    /// </summary>
    private void SpawnDroplet()
    {
        if (outputPrefab == null)
        {
            Debug.LogWarning("[Glass] No hay outputPrefab asignado en el Glass.");
            return;
        }

        LiquidDroplet drop = Instantiate(outputPrefab).GetComponent<LiquidDroplet>();
        if (drop == null)
        {
            Debug.LogWarning("[Glass] El outputPrefab no tiene el componente LiquidDroplet.");
            return;
        }

        // Asignar el LiquidSO y aplicar el color al material de la gota
        ShakerInput shakerInput = drop.GetComponent<ShakerInput>();
        if (shakerInput == null)
            shakerInput = drop.gameObject.AddComponent<ShakerInput>();

        shakerInput.liquidData = _currentLiquid;
        shakerInput.ApplyLiquidColor();

        // Lanzar la gota desde la boca del vaso hacia abajo
        Vector3 spawnPos = pourPoint != null ? pourPoint.position : transform.position;
        drop.SpawnBien(spawnPos, Vector3.down, 10);

        Debug.Log($"[Glass] Gota instanciada con LiquidSO: {_currentLiquid.liquidName}");
    }

    // ─────────────────────────────────────────────────────────────
    //  API PÚBLICA / UTILIDADES
    // ─────────────────────────────────────────────────────────────

    /// <summary>Tipo de líquido que contiene el vaso actualmente (null si está vacío).</summary>
    public LiquidSO CurrentLiquid => _currentLiquid;

    /// <summary>Número de unidades de líquido dentro del vaso.</summary>
    public int LiquidCount => _liquidCount;

    /// <summary>True si el vaso no contiene ningún líquido.</summary>
    public bool IsEmpty => _currentLiquid == null || _liquidCount <= 0;

    /// <summary>
    /// Vacía el vaso completamente sin verter nada (útil para reset de escena).
    /// </summary>
    public void ResetGlass()
    {
        _currentLiquid = null;
        _liquidCount = 0;
        _timePouringWaited = 0f;
        Debug.Log("[Glass] Vaso reiniciado.");
    }
}
