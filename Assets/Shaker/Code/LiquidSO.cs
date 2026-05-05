using UnityEngine;

/// <summary>
/// ScriptableObject que representa un líquido o ingrediente utilizable en el Shaker.
/// Crear instancias desde el menú Assets > Create > Shaker > Liquid.
/// </summary>
[CreateAssetMenu(fileName = "NewLiquid", menuName = "Shaker/Liquid", order = 0)]
public class LiquidSO : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Nombre del líquido o ingrediente")]
    public string liquidName;

    [Tooltip("Descripción del líquido o ingrediente")]
    [TextArea]
    public string description;

    [Header("Visual")]
    [Tooltip("Color representativo del líquido")]
    public Color liquidColor = Color.white;

    [Tooltip("Icono representativo del líquido (opcional)")]
    public Sprite icon;

    [Header("Prefab de salida")]
    [Tooltip("Prefab que se instanciará al expulsar este líquido del shaker")]
    public GameObject outputPrefab;
}
