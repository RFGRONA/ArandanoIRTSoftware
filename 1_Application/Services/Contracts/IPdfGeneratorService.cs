namespace ArandanoIRT.Web._1_Application.Services.Contracts;

public interface IPdfGeneratorService
{
    Task<byte[]> GeneratePlantReportAsync(int plantId, DateTime startDate, DateTime endDate);
}