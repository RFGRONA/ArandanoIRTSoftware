namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IConditionPredictor
{
    Task<int> IsConditionSuitableAsync(float[] modelInput);
}