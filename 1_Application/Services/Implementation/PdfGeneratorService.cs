using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Reports;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Services.Pdf;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ArandanoIRT.Web._1_Application.Services.Implementation;

public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(ApplicationDbContext context, ILogger<PdfGeneratorService> logger)
    {
        _context = context;
        _logger = logger;
        // Configura la licencia de QuestPDF (Community Free)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePlantReportAsync(int plantId, DateTime startDate, DateTime endDate)
    {
        // 1. OBTENER DATOS PRINCIPALES
        var plant = await _context.Plants
            .AsNoTracking()
            .Include(p => p.Crop)
            .FirstOrDefaultAsync(p => p.Id == plantId);

        if (plant == null)
        {
            _logger.LogError("No se pudo generar el reporte: Planta con ID {PlantId} no encontrada.", plantId);
            // Podríamos devolver un PDF de error o lanzar una excepción.
            // Por ahora, devolvemos un array de bytes vacío.
            return Array.Empty<byte>();
        }

        var analysisData = await _context.AnalysisResults
            .AsNoTracking()
            .Where(ar => ar.PlantId == plantId && ar.RecordedAt >= startDate && ar.RecordedAt <= endDate)
            .OrderBy(ar => ar.RecordedAt)
            .Select(ar => new AnalysisResultDataPoint
            {
                Timestamp = ar.RecordedAt,
                CwsiValue = ar.CwsiValue ?? 0,
                CanopyTemperature = ar.CanopyTemperature ?? 0,
                AmbientTemperature = ar.AmbientTemperature ?? 0
            })
            .ToListAsync();
            
        var observationData = await _context.Observations
            .AsNoTracking()
            .Include(o => o.User)
            .Where(o => o.PlantId == plantId && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new ObservationDataPoint
            {
                Timestamp = o.CreatedAt,
                UserName = o.User.FirstName, // Asumiendo que User.FirstName no es nulo
                Description = o.Description
            })
            .ToListAsync();
            
        // 2. CALCULAR MÉTRICAS DE RESUMEN
        var statusHistory = await _context.PlantStatusHistories
            .Where(h => h.PlantId == plantId && h.ChangedAt >= startDate && h.ChangedAt <= endDate)
            .ToListAsync();
            
        var mildStressAlerts = statusHistory.Count(h => h.Status == PlantStatus.MILD_STRESS);
        var severeStressAlerts = statusHistory.Count(h => h.Status == PlantStatus.SEVERE_STRESS);
        var anomalyAlerts = statusHistory.Count(h => h.Status == PlantStatus.UNKNOWN);

        // 3. POBLAR EL MODELO DEL REPORTE
        var reportModel = new PlantReportModel
        {
            PlantName = plant.Name,
            CropName = plant.Crop.Name,
            DateRange = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
            AverageCwsi = analysisData.Any() ? analysisData.Average(ar => ar.CwsiValue) : null,
            MaxCwsi = analysisData.Any() ? analysisData.Max(ar => ar.CwsiValue) : null,
            MildStressAlerts = mildStressAlerts,
            SevereStressAlerts = severeStressAlerts,
            AnomalyAlerts = anomalyAlerts,
            AnalysisData = analysisData,
            ObservationData = observationData
        };

        // 4. GENERAR EL PDF
        _logger.LogInformation("Generando reporte en PDF para la planta {PlantName}", plant.Name);
        var document = new PlantReportDocument(reportModel);
        byte[] pdfBytes = document.GeneratePdf();

        return pdfBytes;
    }
}