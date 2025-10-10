using ArandanoIRT.Web._1_Application.Services.Contracts;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class OnnxConditionPredictor : IConditionPredictor
{
    private readonly InferenceSession _session;

    public OnnxConditionPredictor(string modelPath)
    {
        _session = new InferenceSession(modelPath);
    }

    public async Task<int> IsConditionSuitableAsync(float[] modelInput)
    {
        // El nombre 'float_input' debe coincidir con el definido en la conversión
        var inputName = _session.InputMetadata.Keys.First();

        // Los datos de entrada deben tener la forma [1, 4] para una sola predicción
        var tensor = new DenseTensor<float>(modelInput, new[] { 1, 4 });
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

        // Ejecutar la predicción
        using var results = _session.Run(inputs);
        var prediction = results.FirstOrDefault()?.AsTensor<long>().ToArray()[0];

        return (int)prediction;
    }
}