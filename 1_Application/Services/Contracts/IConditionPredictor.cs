namespace ArandanoIRT.Web._1_Application.Services.Contracts;

/// <summary>
///     Define el contrato para un servicio que predice, utilizando un modelo de machine learning,
///     si las condiciones ambientales actuales son adecuadas para realizar un cálculo válido del CWSI.
/// </summary>
public interface IConditionPredictor
{
    /// <summary>
    ///     Ejecuta el modelo de predicción con los datos de entrada proporcionados.
    /// </summary>
    /// <param name="modelInput">Un arreglo de flotantes (tensor) que representa las características de entrada para el modelo.</param>
    /// <returns>Un entero que representa la clase predicha (ej. 1 para condiciones adecuadas, 0 para no adecuadas).</returns>
    Task<int> IsConditionSuitableAsync(float[] modelInput);
}