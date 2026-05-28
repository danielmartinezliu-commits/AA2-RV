using UnityEngine;

/// <summary>
/// Niveles de mezcla disponibles en el Shaker.
/// El porcentaje entre paréntesis indica el rango del medidor de shake necesario.
/// </summary>
public enum MixLevel
{
    /// <summary>Sin mezclar (0 - 19%)</summary>
    Unmixed = 0,
    Mixed = 1
}

/// <summary>
/// ScriptableObject que define una receta del Shaker:
/// qué ingredientes se necesitan, el nivel de mezcla requerido y qué se obtiene como resultado.
/// Crear instancias desde el menú Assets > Create > Shaker > Recipe.
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Shaker/Recipe", order = 1)]
public class ShakerRecipeSO : ScriptableObject
{
    [Header("Ingredientes")]
    [Tooltip("Array de LiquidSO que forman los ingredientes de esta receta. " +
             "El orden NO importa; la comprobación se hace por contenido.")]
    public LiquidSO[] ingredients;

    [Header("Nivel de mezcla requerido")]
    [Tooltip("Nivel mínimo de mezcla necesario para obtener el resultado de esta receta")]
    public MixLevel requiredMixLevel;

    [Header("Resultado")]
    [Tooltip("LiquidSO que se obtendrá al cumplir los requisitos de esta receta")]
    public LiquidSO result;

    /// <summary>
    /// Comprueba si la lista de ingredientes proporcionada y el nivel de mezcla coinciden con esta receta.
    /// La comparación de ingredientes es independiente del orden.
    /// </summary>
    /// <param name="inputIngredients">Lista de LiquidSO que hay dentro del shaker</param>
    /// <param name="currentMixLevel">Nivel de mezcla actual del shaker</param>
    /// <returns>True si los ingredientes y el nivel de mezcla coinciden con la receta</returns>
    public bool Matches(System.Collections.Generic.List<LiquidSO> inputIngredients, MixLevel currentMixLevel)
    {
        // Verificar que el nivel de mezcla sea suficiente
        if (currentMixLevel < requiredMixLevel)
            return false;

        // Copiar la lista de entrada para poder marcar los ingredientes ya usados sin modificar el original
        var remaining = new System.Collections.Generic.List<LiquidSO>(inputIngredients);

        foreach (var required in ingredients)
        {
            // Buscar el ingrediente requerido en lo que queda de la lista
            int index = remaining.IndexOf(required);
            if (index < 0)
                return false; // Falta algún ingrediente requerido por la receta

            // Marcarlo como usado para respetar duplicados en la receta (ej: 2 limones → necesita al menos 2)
            remaining.RemoveAt(index);
        }

        // Todos los ingredientes requeridos estaban presentes; los sobrantes en el shaker se ignoran
        return true;
    }
}