using System.Data.Common;
using UnityEngine;

/// <summary>
/// Script que debe colocarse en el GameObject hijo que representa la boca del vaso (Trigger).
/// Detecta objetos con ShakerInput y los pasa al Glass padre, igual que ShakerTopCollider hace con el Shaker.
///
/// Setup recomendado:
///   Glass (GameObject principal con el script Glass.cs)
///   └── TopCollider (este GameObject, con Collider en modo IsTrigger = true)
/// </summary>
[RequireComponent(typeof(Collider))]
public class GlassTopCollider : MonoBehaviour
{
    [Tooltip("Referencia al script Glass del objeto padre. Se asigna automáticamente si se deja vacío.")]
    public Glass parentGlass;

    private void Awake()
    {
        // Asegurarse de que el collider está en modo Trigger
        GetComponent<Collider>().isTrigger = true;

        // Buscar el Glass en el padre si no está asignado
        if (parentGlass == null)
        {
            parentGlass = GetComponentInParent<Glass>();

            if (parentGlass == null)
            {
                Debug.LogError("[GlassTopCollider] No se encontró el componente Glass en el padre. " +
                               "Asegúrate de que este objeto es hijo del Glass.");
            }
        }
    }

    /// <summary>
    /// Se dispara cuando otro collider entra en la zona trigger de la boca del vaso.
    /// Comprueba si el objeto tiene ShakerInput y llama al Glass para intentar añadirlo.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("AAAAAAAA");
        if (parentGlass == null)
            return;

        ShakerInput shakerInput = other.GetComponent<ShakerInput>();
        if (shakerInput != null)
        {
            Debug.Log("[GlassTopCollider] Objeto detectado: " + shakerInput.liquidData.name);
            parentGlass.TryAddLiquid(other.gameObject);
        }
    }
}
