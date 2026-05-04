using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Makes the bottle grabbable in VR via XR Interaction Toolkit.
///
/// Required components (added automatically via RequireComponent):
///   · Rigidbody          – physics + throw on release
///   · XRGrabInteractable – XRIT grab logic
///
/// Optional: assign LiquidController to get notified on grab/release.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class BottleGrabbable : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign if the liquid mesh lives on a child object.")]
    [SerializeField] LiquidController liquidController;

    [Header("Grab settings")]
    [Tooltip("VelocityTracking = physics feel. Kinematic = 1:1 hand tracking.")]
    [SerializeField] XRBaseInteractable.MovementType movementType =
        XRBaseInteractable.MovementType.VelocityTracking;

    [Tooltip("Throw the bottle on release using hand velocity.")]
    [SerializeField] bool throwOnDetach = true;

    // ── References cached at Awake ────────────────────────────────────────────

    XRGrabInteractable _grab;
    Rigidbody          _rb;

    // ── Unity callbacks ───────────────────────────────────────────────────────

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        // Apply settings (can still be overridden in the Inspector afterwards)
        _grab.movementType  = movementType;
        _grab.throwOnDetach = throwOnDetach;

        _grab.selectEntered.AddListener(OnGrabbed);
        _grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (_grab == null) return;
        _grab.selectEntered.RemoveListener(OnGrabbed);
        _grab.selectExited.RemoveListener(OnReleased);
    }

    // ── Grab events ───────────────────────────────────────────────────────────

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Nothing extra needed — LiquidController.Update() already reads
        // transform rotation every frame, so the wobble kicks in automatically.
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Could trigger a pour-stop here when pouring is implemented.
    }

    // ── Public helpers (for future pouring logic etc.) ────────────────────────

    public bool IsGrabbed => _grab != null && _grab.isSelected;
}
