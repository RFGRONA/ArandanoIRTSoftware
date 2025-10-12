using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

/// <summary>
///     Implementación del predictor de condiciones que utiliza un modelo de machine learning en formato ONNX.
///     Carga el modelo y ejecuta inferencias para realizar predicciones.
/// </summary>
public class OnnxConditionPredictor : IConditionPredictor
{
    private readonly InferenceSession _session;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="OnnxConditionPredictor" />,
    ///     cargando el modelo ONNX desde la ruta especificada.
    /// </summary>
    /// <param name="modelPath">La ruta al archivo del modelo .onnx.</param>
    public OnnxConditionPredictor(string modelPath)
    {
        _session = new InferenceSession(modelPath);
    }

    /// <inheritdoc />
    public async Task<int> IsConditionSuitableAsync(float[] modelInput)
    {
        // El nombre de entrada (ej. 'float_input') debe coincidir exactamente con el nombre de entrada del modelo ONNX.
        var inputName = _session.InputMetadata.Keys.First();

        // Los datos de entrada se deben conformar a la forma que espera el modelo (en este caso, [1, 4] para una predicción).
        var tensor = new DenseTensor<float>(modelInput, new[] { 1, 4 });
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        using var results = _session.Run(inputs);
        var prediction = results.FirstOrDefault()?.AsTensor<long>().ToArray()[0];

        return (int)prediction;
    }
}