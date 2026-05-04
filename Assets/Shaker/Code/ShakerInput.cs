using UnityEngine;

/// <summary>
/// Script que se adjunta a los objetos físicos que pueden ser introducidos en el Shaker.
/// Actúa como portador de la información del líquido/ingrediente que representa el objeto.
/// </summary>
public class ShakerInput : MonoBehaviour
{
    [Header("Datos del ingrediente")]
    [Tooltip("Referencia al ScriptableObject que define qué líquido/ingrediente es este objeto")]
    public LiquidSO liquidData;

    /// <summary>
    /// Devuelve el LiquidSO asignado a este objeto.
    /// </summary>
    public LiquidSO GetLiquidData() => liquidData;
}