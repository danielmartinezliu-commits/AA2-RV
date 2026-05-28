using UnityEngine;

/// <summary>
/// ScriptableObject que representa un líquido o ingrediente utilizable en el Shaker.
/// Crear instancias desde el menú Assets > Create > Shaker > Liquid.
/// </summary>
[CreateAssetMenu(fileName = "NewLiquid", menuName = "Shaker/Liquid", order = 0)]
public class LiquidSO : ScriptableObject
{
    [Header("Identificación")]
    public string liquidName;

    [Header("Visual")]
    public Color liquidColor = Color.white;
}