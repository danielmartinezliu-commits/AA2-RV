using UnityEngine;

/// <summary>
/// Niveles de mezcla disponibles en el Shaker.
/// El porcentaje entre paréntesis indica el rango del medidor de shake necesario.
/// </summary>
public enum MixLevel
{
    /// <summary>Sin mezclar (0 - 19%)</summary>
    Unmixed = 0,

    /// <summary>Poco mezclado (20 - 39%)</summary>
    SlightlyMixed = 1,

    /// <summary>Mezclado (40 - 59%)</summary>
    Mixed = 2,

    /// <summary>Bastante mezclado (60 - 79%)</summary>
    WellMixed = 3,

    /// <summary>Muy mezclado (80 - 100%)</summary>
    FullyMixed = 4
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
    public LiquidSO[] ingredients;

    [Header("Nivel de mezcla requerido")]
    public MixLevel requiredMixLevel;

    [Header("Resultado")]
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

        // Verificar que la cantidad de ingredientes coincida
        if (inputIngredients.Count != ingredients.Length)
            return false;

        // Copiar la lista de entrada para poder marcar los ingredientes ya usados
        var remaining = new System.Collections.Generic.List<LiquidSO>(inputIngredients);

        foreach (var required in ingredients)
        {
            // Buscar el ingrediente requerido en la lista restante
            int index = remaining.IndexOf(required);
            if (index < 0)
                return false; // Ingrediente no encontrado

            remaining.RemoveAt(index); // Marcar como usado
        }

        // Si todos los ingredientes fueron encontrados y no sobra ninguno, la receta coincide
        return remaining.Count == 0;
    }
}