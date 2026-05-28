using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
    public ShakerRecipeSO[] allRecipes;

    public LiquidSO genericLiquid;

    [Header("Tapa")]
    public ShakerLid shakerLid;

    [Header("Vertido")]
    public float pourAngleThreshold = 100f;

    public Transform pourPoint;

    public GameObject outputPrefab;

    [Header("Medidor de Shake")]
    public float movementDeltaThreshold = 0.05f;

    public float rotationDeltaThreshold = 5f;

    public float shakeSensitivity = 1f;
    public Transform shakeBar;
    public Image shakeProcess;
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
    private XRGrabInteractable _grabInteractable;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();

        shakeBar.GetComponentInParent<Canvas>().worldCamera = Camera.main;
    }

    private void Start()
    {
        // Guardar la posición y rotación inicial como base del delta
        _previousPosition = transform.position;
        _previousRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        if (_grabInteractable.isSelected)
            HandleShaking();
        HandlePouring();
    }

    private void Update()
    {
        bool active = _grabInteractable.isSelected && _lidAttached;
        shakeBar.gameObject.SetActive(active);
        if (active)
        {
            //shakeBar.transform.position = transform.position + new Vector3(0, 1.5f);
        }
    }


    // ─────────────────────────────────────────────────────────────
    //  TAPA  (notificaciones entrantes desde ShakerLid)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por ShakerLid cuando la tapa se adhiere al shaker.
    /// </summary>
    public void NotifyLidAttached()
    {
        _lidAttached = true;
        _lidEverAttached = true;
        Debug.Log("[Shaker] Tapa adherida notificada.");
    }

    /// <summary>
    /// Llamado por ShakerLid cuando la tapa se retira del shaker.
    /// </summary>
    public void NotifyLidDetached()
    {
        _lidAttached = false;
        // Al quitar la tapa se permite volver a verter en el siguiente ciclo
        _hasPouredContent = false;
        Debug.Log("[Shaker] Tapa retirada notificada.");
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
            Debug.Log("SE ME MUEVEEEEE " + _shakeMeter);
        }

        // Actualizar posición y rotación previas para el siguiente frame
        _previousPosition = currentPosition;
        _previousRotation = currentRotation;


        shakeProcess.fillAmount = _shakeMeter / 100;
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
    /// Instancia el prefab de salida del propio Shaker en la boca del shaker,
    /// asigna el LiquidSO al ShakerInput y aplica el color del líquido al material.
    /// </summary>
    /// <param name="liquid">LiquidSO que se asignará al objeto instanciado</param>
    private void SpawnOutput(LiquidSO liquid)
    {
        if (outputPrefab == null)
        {
            Debug.LogWarning("[Shaker] No hay outputPrefab asignado en el Shaker.");
            return;
        }

        Vector3 spawnPosition = pourPoint != null ? pourPoint.position : transform.position;

        GameObject spawnedObject = Instantiate(outputPrefab, spawnPosition, Quaternion.identity);

        // Obtener o añadir el ShakerInput al objeto instanciado
        ShakerInput shakerInput = spawnedObject.GetComponent<ShakerInput>();
        if (shakerInput == null)
            shakerInput = spawnedObject.AddComponent<ShakerInput>();

        // Asignar el LiquidSO y aplicar el color al material (inicialización manual
        // porque Awake ya se ejecutó al instanciar antes de que asignemos liquidData)
        shakerInput.liquidData = liquid;
        shakerInput.ApplyLiquidColor();

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
    /// Si hay un ShakerLid asignado se consulta directamente su estado.
    /// </summary>
    public bool IsLidAttached() => shakerLid != null ? shakerLid.IsAttached : _lidAttached;

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