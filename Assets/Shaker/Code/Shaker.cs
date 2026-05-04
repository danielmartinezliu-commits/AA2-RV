using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script principal del Shaker de bar para VR.
/// Gestiona la introducción de ingredientes, el cálculo del shake, la tapa y el vertido del resultado.
/// Requiere que el GameObject tenga un Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Shaker : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Recetas")]
    [Tooltip("Lista de todas las recetas disponibles que el shaker puede producir")]
    public ShakerRecipeSO[] allRecipes;

    [Tooltip("LiquidSO genérico que se devuelve cuando el contenido no coincide con ninguna receta")]
    public LiquidSO genericLiquid;

    [Header("Tapa")]
    [Tooltip("Transform de la tapa del shaker (objeto separado con su propio grabable de VR)")]
    public Transform lid;

    [Tooltip("Transform del punto de anclaje de la tapa en la parte superior del shaker")]
    public Transform lidAnchor;

    [Tooltip("Distancia máxima a la que la tapa se adhiere automáticamente al shaker (en metros)")]
    public float lidSnapDistance = 0.05f;

    [Header("Vertido")]
    [Tooltip("Ángulo de inclinación (en grados) a partir del cual el shaker vierte su contenido (sin tapa)")]
    public float pourAngleThreshold = 100f;

    [Tooltip("Punto de spawn de los objetos vertidos (parte superior / boca del shaker)")]
    public Transform pourPoint;

    [Header("Medidor de Shake")]
    [Tooltip("Umbral mínimo de delta de movimiento (metros/frame) para que empiece a sumar al medidor")]
    public float movementDeltaThreshold = 0.05f;

    [Tooltip("Umbral mínimo de delta de rotación (grados/frame) para que empiece a sumar al medidor")]
    public float rotationDeltaThreshold = 5f;

    [Tooltip("Multiplicador que escala cuánto suman los deltas al medidor de shake")]
    public float shakeSensitivity = 1f;

    // ─────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────

    /// <summary>Lista de ingredientes actualmente dentro del shaker</summary>
    private List<LiquidSO> _contents = new List<LiquidSO>();

    /// <summary>Medidor de shake, rango 0-100</summary>
    private float _shakeMeter = 0f;

    /// <summary>Indica si la tapa está colocada en este momento</summary>
    private bool _lidAttached = false;

    /// <summary>Indica si la tapa ha sido colocada en algún momento (para decidir si se puede mezclar)</summary>
    private bool _lidEverAttached = false;

    /// <summary>Indica si ya se ha vertido el contenido en este ciclo (evita volver a verter cada frame)</summary>
    private bool _hasPouredContent = false;

    // Posición y rotación del frame anterior para calcular deltas
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;

    private Rigidbody _rb;

    // ─────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Guardar la posición y rotación inicial como base del delta
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        HandleLidSnapping();
        HandleShaking();
        HandlePouring();
    }

    // ─────────────────────────────────────────────────────────────
    //  TAPA
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Comprueba si la tapa suelta está lo suficientemente cerca del punto de anclaje
    /// para adherirse automáticamente al shaker.
    /// </summary>
    private void HandleLidSnapping()
    {
        if (lid == null || lidAnchor == null)
            return;

        // Si la tapa ya está adherida, verificar si el jugador la está agarrando (para despegarla)
        if (_lidAttached)
        {
            // La tapa se desadhiere cuando el jugador la agarra: la lógica de grab de VR
            // debe llamar a DetachLid() al cogerla. No se gestiona aquí para separar responsabilidades.
            return;
        }

        // Comprobar distancia entre la tapa y el punto de anclaje
        float distance = Vector3.Distance(lid.position, lidAnchor.position);
        if (distance <= lidSnapDistance)
        {
            AttachLid();
        }
    }

    /// <summary>
    /// Adhiere la tapa al shaker, bloqueando su Transform al punto de anclaje.
    /// </summary>
    public void AttachLid()
    {
        if (lid == null || lidAnchor == null)
            return;

        _lidAttached = true;
        _lidEverAttached = true;

        // Emparentar la tapa al shaker y ajustar su Transform al ancla
        lid.SetParent(transform);
        lid.localPosition = lidAnchor.localPosition;
        lid.localRotation = lidAnchor.localRotation;

        // Desactivar la física de la tapa mientras está adherida
        Rigidbody lidRb = lid.GetComponent<Rigidbody>();
        if (lidRb != null)
        {
            lidRb.isKinematic = true;
        }

        Debug.Log("[Shaker] Tapa adherida al shaker.");
    }

    /// <summary>
    /// Desadhiere la tapa del shaker (llamar desde el sistema de grab de VR cuando el jugador la coge).
    /// </summary>
    public void DetachLid()
    {
        if (!_lidAttached || lid == null)
            return;

        _lidAttached = false;

        // Desemparentar la tapa
        lid.SetParent(null);

        // Reactivar la física de la tapa
        Rigidbody lidRb = lid.GetComponent<Rigidbody>();
        if (lidRb != null)
        {
            lidRb.isKinematic = false;
        }

        // Al quitar la tapa se resetea el estado de vertido para permitir verter de nuevo
        _hasPouredContent = false;

        Debug.Log("[Shaker] Tapa retirada del shaker.");
    }

    // ─────────────────────────────────────────────────────────────
    //  INTRODUCCIÓN DE INGREDIENTES (llamada desde la colisión superior)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Se llama cuando un objeto colisiona con la zona de entrada superior del shaker.
    /// Si el objeto tiene un ShakerInput y la tapa no está puesta, absorbe su LiquidSO y destruye el objeto.
    /// Este método debe ser invocado desde el script de la colisión superior (ShakerTopCollider).
    /// </summary>
    /// <param name="inputObject">GameObject que ha entrado en la zona superior</param>
    public void TryAddIngredient(GameObject inputObject)
    {
        // Solo se pueden añadir ingredientes si la tapa no está puesta
        if (_lidAttached)
        {
            Debug.Log("[Shaker] No se puede añadir ingrediente: la tapa está puesta.");
            return;
        }

        ShakerInput shakerInput = inputObject.GetComponent<ShakerInput>();
        if (shakerInput == null)
        {
            Debug.Log("[Shaker] El objeto no tiene ShakerInput, ignorando.");
            return;
        }

        if (shakerInput.liquidData == null)
        {
            Debug.LogWarning("[Shaker] El ShakerInput no tiene LiquidSO asignado.");
            return;
        }

        // Añadir el ingrediente a la lista y destruir el objeto físico
        _contents.Add(shakerInput.liquidData);
        Debug.Log($"[Shaker] Ingrediente añadido: {shakerInput.liquidData.liquidName}. " +
                  $"Total ingredientes: {_contents.Count}");

        Destroy(inputObject);
    }

    // ─────────────────────────────────────────────────────────────
    //  MEZCLA (SHAKE)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula el delta de movimiento y rotación del shaker en cada FixedUpdate
    /// y acumula el valor en el medidor de shake si la tapa está puesta.
    /// </summary>
    private void HandleShaking()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        if (_lidAttached && _contents.Count > 0)
        {
            // Calcular deltas
            float movementDelta = Vector3.Distance(currentPosition, _previousPosition);
            float rotationDelta = Quaternion.Angle(currentRotation, _previousRotation);

            // Solo sumar si superan los umbrales definidos
            float contribution = 0f;

            if (movementDelta > movementDeltaThreshold)
            {
                // Contribución logarítmica del movimiento
                contribution += Mathf.Log(1f + movementDelta) * shakeSensitivity;
            }

            if (rotationDelta > rotationDeltaThreshold)
            {
                // Contribución logarítmica de la rotación
                contribution += Mathf.Log(1f + rotationDelta) * shakeSensitivity;
            }

            _shakeMeter = Mathf.Clamp(_shakeMeter + contribution, 0f, 100f);
        }

        // Actualizar posición y rotación previas para el siguiente frame
        _previousPosition = currentPosition;
        _previousRotation = currentRotation;
    }

    /// <summary>
    /// Devuelve el nivel de mezcla actual basado en el valor del medidor de shake.
    /// </summary>
    public MixLevel GetCurrentMixLevel()
    {
        if (_shakeMeter < 20f) return MixLevel.Unmixed;
        if (_shakeMeter < 40f) return MixLevel.SlightlyMixed;
        if (_shakeMeter < 60f) return MixLevel.Mixed;
        if (_shakeMeter < 80f) return MixLevel.WellMixed;
        return MixLevel.FullyMixed;
    }

    /// <summary>
    /// Devuelve el valor actual del medidor de shake (0-100).
    /// </summary>
    public float GetShakeMeterValue() => _shakeMeter;

    // ─────────────────────────────────────────────────────────────
    //  VERTIDO
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Comprueba si el shaker está suficientemente inclinado para verter su contenido.
    /// Solo actúa si la tapa no está puesta y hay ingredientes dentro.
    /// </summary>
    private void HandlePouring()
    {
        // No verter si la tapa está puesta, si no hay contenido o si ya se vertió en este ciclo
        if (_lidAttached || _contents.Count == 0 || _hasPouredContent)
            return;

        // Calcular el ángulo entre el eje "arriba" del shaker y el "arriba" del mundo
        float angle = Vector3.Angle(transform.up, Vector3.up);

        if (angle >= pourAngleThreshold)
        {
            PourContent();
        }
    }

    /// <summary>
    /// Determina qué LiquidSO debe producirse, instancia el prefab correspondiente
    /// y limpia el contenido del shaker.
    /// </summary>
    private void PourContent()
    {
        _hasPouredContent = true;

        LiquidSO outputLiquid = ResolveRecipe();

        if (outputLiquid == null)
        {
            Debug.LogWarning("[Shaker] No se encontró resultado (ni genérico). Asigna genericLiquid en el Inspector.");
            return;
        }

        Debug.Log($"[Shaker] Vertiendo: {outputLiquid.liquidName} " +
                  $"(Mezcla: {GetCurrentMixLevel()}, Medidor: {_shakeMeter:F1})");

        SpawnOutput(outputLiquid);

        // Limpiar el estado del shaker tras verter
        _contents.Clear();
        _shakeMeter = 0f;
        _lidEverAttached = false;
    }

    /// <summary>
    /// Busca en todas las recetas disponibles cuál coincide con el contenido y nivel de mezcla actuales.
    /// Si no hay coincidencia devuelve el líquido genérico.
    /// Si la tapa nunca estuvo puesta o el nivel de mezcla es Unmixed devuelve los ingredientes sin mezclar
    /// (en ese caso se devuelve el genérico como fallback, ya que los objetos individuales ya fueron destruidos).
    /// </summary>
    private LiquidSO ResolveRecipe()
    {
        MixLevel currentMix = GetCurrentMixLevel();

        // Si la tapa nunca se puso no se pudo mezclar: devolver genérico directamente
        if (!_lidEverAttached)
        {
            Debug.Log("[Shaker] La tapa nunca fue colocada; devolviendo líquido genérico (sin mezclar).");
            return genericLiquid;
        }

        // Comprobar cada receta
        foreach (ShakerRecipeSO recipe in allRecipes)
        {
            if (recipe == null)
                continue;

            if (recipe.Matches(_contents, currentMix))
            {
                Debug.Log($"[Shaker] Receta encontrada: {recipe.name} → {recipe.result.liquidName}");
                return recipe.result;
            }
        }

        // Ninguna receta coincide → devolver el líquido genérico
        Debug.Log("[Shaker] Ninguna receta coincide; devolviendo líquido genérico.");
        return genericLiquid;
    }

    /// <summary>
    /// Instancia el prefab de salida del LiquidSO en la boca del shaker
    /// y le asigna su ShakerInput correspondiente.
    /// </summary>
    /// <param name="liquid">LiquidSO cuyo prefab se instanciará</param>
    private void SpawnOutput(LiquidSO liquid)
    {
        if (liquid.outputPrefab == null)
        {
            Debug.LogWarning($"[Shaker] El LiquidSO '{liquid.liquidName}' no tiene outputPrefab asignado.");
            return;
        }

        Vector3 spawnPosition = pourPoint != null ? pourPoint.position : transform.position;
        Quaternion spawnRotation = Quaternion.identity;

        GameObject spawnedObject = Instantiate(liquid.outputPrefab, spawnPosition, spawnRotation);

        // Asegurarse de que el objeto instanciado tiene ShakerInput y asignar el LiquidSO
        ShakerInput shakerInput = spawnedObject.GetComponent<ShakerInput>();
        if (shakerInput == null)
        {
            shakerInput = spawnedObject.AddComponent<ShakerInput>();
        }

        shakerInput.liquidData = liquid;

        Debug.Log($"[Shaker] Objeto instanciado: {spawnedObject.name} con LiquidSO: {liquid.liquidName}");
    }

    // ─────────────────────────────────────────────────────────────
    //  API PÚBLICA / UTILIDADES
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve una copia de la lista de ingredientes actuales (solo lectura).
    /// </summary>
    public IReadOnlyList<LiquidSO> GetContents() => _contents.AsReadOnly();

    /// <summary>
    /// Indica si la tapa está actualmente adherida al shaker.
    /// </summary>
    public bool IsLidAttached() => _lidAttached;

    /// <summary>
    /// Reinicia completamente el estado del shaker (vacía el contenido y el medidor de shake).
    /// </summary>
    public void ResetShaker()
    {
        _contents.Clear();
        _shakeMeter = 0f;
        _lidEverAttached = false;
        _hasPouredContent = false;
        Debug.Log("[Shaker] Estado del shaker reiniciado.");
    }
}