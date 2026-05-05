using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Gestiona todo el comportamiento de la tapa del Shaker con XR Interaction Toolkit.
///
/// Comportamiento esperado:
///  - Cuando la tapa no está en la mano y entra en rango del shaker → se adhiere (snap)
///  - Cuando se adhiere y el jugador la coge con el mando → se desadhiere y se fuerza el grab
///  - Mientras la tapa está siendo sostenida en la mano → el snap queda bloqueado hasta que
///    salga del rango y vuelva a entrar (evita re-adherirse inmediatamente al soltar)
///
/// Setup requerido:
///  - Este script va en el mismo GameObject que el XRGrabInteractable de la tapa
///  - El shaker necesita un SphereCollider (isTrigger = true) que cubra la zona de snap,
///    o se puede usar la detección por distancia en Update (modo configurado con useProximityCheck)
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ShakerLid : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Referencias")]
    public Shaker targetShaker;

    public Transform lidAnchor;

    [Header("Snap")]
    public float snapDistance = 0.06f;
    public float snapResetDistance = 0.12f;

    // ─────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────

    /// <summary>La tapa está físicamente adherida al shaker en este momento</summary>
    private bool _isAttached = false;

    /// <summary>
    /// El snap está bloqueado porque la tapa está (o estuvo) en la mano y aún no ha salido del rango.
    /// Se activa al cogerla y se desactiva cuando supera snapResetDistance.
    /// </summary>
    private bool _snapBlocked = false;

    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;

    // ─────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();

        // Suscribirse a los eventos de grab y release del XRGrabInteractable
        _grabInteractable.selectEntered.AddListener(OnGrabbed);
        _grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Update()
    {
        if (lidAnchor == null)
            return;

        float distanceToAnchor = Vector3.Distance(transform.position, lidAnchor.position);

        if (_isAttached)
        {
            transform.position = lidAnchor.transform.position;
            transform.rotation = lidAnchor.transform.rotation;
            return;
        }

        // Gestión del bloqueo de snap: desbloquear cuando la tapa se aleje suficiente
        if (_snapBlocked)
        {
            if (distanceToAnchor >= snapResetDistance)
            {
                _snapBlocked = false;
                //Debug.Log("[ShakerLid] Snap desbloqueado: la tapa ha salido del rango de reset.");
            }
            return; // Mientras está bloqueado no intentar snap
        }

        // Intentar snap si la tapa está en rango y no está siendo sostenida
        bool isBeingHeld = _grabInteractable.isSelected;
        if (!isBeingHeld && distanceToAnchor <= snapDistance)
        {
            AttachToShaker();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  EVENTOS DE GRAB / RELEASE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Se dispara cuando el jugador coge la tapa con el mando.
    /// Si estaba adherida al shaker, la desadhiere primero.
    /// Bloquea el snap para que no vuelva a adherirse inmediatamente.
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Bloquear el snap en cuanto se coge (aunque no estuviera adherida)
        _snapBlocked = true;

        if (_isAttached)
        {
            DetachFromShaker();
            //Debug.Log("[ShakerLid] Tapa cogida mientras estaba adherida → desadherida.");
        }
        else
        {
            //Debug.Log("[ShakerLid] Tapa cogida (no estaba adherida).");
        }
    }

    /// <summary>
    /// Se dispara cuando el jugador suelta la tapa.
    /// El snap seguirá bloqueado hasta que la tapa se aleje suficiente del anchor (ver Update).
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        //Debug.Log("[ShakerLid] Tapa soltada. Snap bloqueado hasta salir del rango de reset.");
        if (!IsAttached)
        {
            transform.SetParent(null);
            _rb.isKinematic = false;
        }
        // _snapBlocked ya está en true; se desactivará en Update cuando supere snapResetDistance
    }

    // ─────────────────────────────────────────────────────────────
    //  ADHERIR / DESadherir
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Adhiere la tapa al punto de anclaje del shaker:
    /// - Desactiva la interacción de grab temporalmente
    /// - Hace la tapa kinematic
    /// - La emparenta al anchor
    /// - Notifica al Shaker
    /// </summary>
    private void AttachToShaker()
    {
        _isAttached = true;

        // Desactivar el grab para que no se pueda coger mientras está "encajada"
        // pero dejar el interactable activo para poder detectar cuando el jugador la coge
        // (el evento selectEntered seguirá funcionando aunque trackedDeviceGraphicRaycaster esté activo)
        // Usamos interactionLayers o directamente deshabilitamos el collider de grab si se necesita.
        // Por simplicidad lo dejamos activo: el jugador podrá cogerla para retirarla.

        // Hacer kinematic para que no le afecte la física
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Emparentar al shaker y colocar en el punto de anclaje
        transform.SetParent(lidAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Notificar al Shaker
        if (targetShaker != null)
            targetShaker.NotifyLidAttached();

        Debug.Log("[ShakerLid] Tapa adherida al shaker.");
    }

    /// <summary>
    /// Desadhiere la tapa del shaker:
    /// - Desemparenta
    /// - Reactiva la física
    /// - Notifica al Shaker
    /// </summary>
    private void DetachFromShaker()
    {
        _isAttached = false;

        // Desemparentar del anchor
        transform.SetParent(null);

        // Reactivar física
        _rb.isKinematic = false;

        // Notificar al Shaker
        if (targetShaker != null)
            targetShaker.NotifyLidDetached();

        Debug.Log("[ShakerLid] Tapa desadherida del shaker.");
    }

    // ─────────────────────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────────────────────

    /// <summary>Indica si la tapa está adherida al shaker en este momento.</summary>
    public bool IsAttached => _isAttached;
}