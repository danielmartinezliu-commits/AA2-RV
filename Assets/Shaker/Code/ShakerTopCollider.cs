using UnityEngine;

/// <summary>
/// Script que debe colocarse en el GameObject hijo que representa la zona de entrada
/// de la parte superior del Shaker (collider en modo Trigger).
/// Detecta objetos con ShakerInput y los pasa al Shaker padre.
/// 
/// Setup recomendado:
///   Shaker (GameObject principal con el script Shaker.cs)
///   └── TopCollider (este GameObject, con Collider en modo IsTrigger = true)
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShakerTopCollider : MonoBehaviour
{
    /// <summary>Referencia al Shaker padre (se busca automáticamente si no se asigna)</summary>
    [Tooltip("Referencia al script Shaker del objeto padre. Se asigna automáticamente si se deja vacío.")]
    public Shaker parentShaker;

    private void Awake()
    {
        // Asegurarse de que el collider está en modo Trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Buscar el Shaker en el padre si no está asignado
        if (parentShaker == null)
        {
            parentShaker = GetComponentInParent<Shaker>();

            if (parentShaker == null)
            {
                Debug.LogError("[ShakerTopCollider] No se encontró el componente Shaker en el padre. " +
                               "Asegúrate de que este objeto es hijo del Shaker.");
            }
        }
    }

    /// <summary>
    /// Se dispara cuando otro collider entra en la zona trigger superior.
    /// Comprueba si el objeto tiene ShakerInput y llama al Shaker para absorberlo.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (parentShaker == null)
            return;

        // Verificar que el objeto tiene ShakerInput antes de pasarlo al shaker
        ShakerInput shakerInput = other.GetComponent<ShakerInput>();
        if (shakerInput != null)
        {
            parentShaker.TryAddIngredient(other.gameObject);
        }
    }
}