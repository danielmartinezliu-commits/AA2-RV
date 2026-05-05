using UnityEngine;

/// <summary>
/// Script que se adjunta a los objetos físicos que pueden ser introducidos en el Shaker.
/// Actúa como portador de la información del líquido/ingrediente que representa el objeto.
/// Al inicializarse, aplica el color del LiquidSO asignado a la instancia de material del Renderer.
/// </summary>
public class ShakerInput : MonoBehaviour
{
    [Header("Datos del ingrediente")]
    public LiquidSO liquidData;

    [Header("Renderer")]
    public Renderer targetRenderer;

    private void Awake()
    {
        ApplyLiquidColor();
    }

    /// <summary>
    /// Aplica el color del LiquidSO asignado a la instancia de material del Renderer.
    /// Se puede llamar manualmente si se asigna el liquidData en tiempo de ejecución tras el Awake.
    /// </summary>
    public void ApplyLiquidColor()
    {
        if (liquidData == null)
        {
            Debug.LogWarning($"[ShakerInput] '{gameObject.name}' no tiene LiquidSO asignado; no se aplica color.");
            return;
        }

        // Buscar el Renderer automáticamente si no está asignado en el Inspector
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogWarning($"[ShakerInput] '{gameObject.name}' no tiene Renderer; no se puede aplicar color.");
            return;
        }

        // Editar la instancia del material (no el asset compartido) para no afectar a otros objetos
        targetRenderer.materials[0].color = liquidData.liquidColor;
    }

    /// <summary>
    /// Devuelve el LiquidSO asignado a este objeto.
    /// </summary>
    public LiquidSO GetLiquidData() => liquidData;
}