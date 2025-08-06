using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._0_Domain.Enums;
using ArandanoIRT.Web._1_Application.DTOs.Reports;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._2_Infrastructure.Data;
using ArandanoIRT.Web._2_Infrastructure.Services.Pdf;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
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
        Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePlantReportAsync(int plantId, DateTime startDate, DateTime endDate)
    {
        var queryStartDate = startDate.Date.ToSafeUniversalTime();
        var queryEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToSafeUniversalTime();

        var plant = await _context.Plants
            .AsNoTracking()
            .Include(p => p.Crop)
            .FirstOrDefaultAsync(p => p.Id == plantId);

        if (plant == null)
        {
            _logger.LogError("No se pudo generar el reporte: Planta con ID {PlantId} no encontrada.", plantId);
            return Array.Empty<byte>();
        }

        var analysisData = await _context.AnalysisResults
            .AsNoTracking()
            .Where(ar => ar.PlantId == plantId && ar.RecordedAt >= queryStartDate && ar.RecordedAt < queryEndDate)
            .OrderBy(ar => ar.RecordedAt)
            .Select(ar => new AnalysisResultDataPoint
            {
                Timestamp = ar.RecordedAt,
                CwsiValue = ar.CwsiValue ?? 0,
                CanopyTemperature = ar.CanopyTemperature ?? 0,
                AmbientTemperature = ar.AmbientTemperature ?? 0
            })
            .ToListAsync();

        // --- INICIO DE LA CORRECCIÓN: OBTENER TODOS LOS DATOS ---
        var observationData = await _context.Observations
            .AsNoTracking()
            .Include(o => o.User)
            .Where(o => o.PlantId == plantId && o.CreatedAt >= queryStartDate && o.CreatedAt < queryEndDate)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new ObservationDataPoint
            {
                Timestamp = o.CreatedAt,
                UserName = o.User.FirstName,
                Description = o.Description
            })
            .ToListAsync();

        var statusHistory = await _context.PlantStatusHistories
            .Where(h => h.PlantId == plantId && h.ChangedAt >= queryStartDate && h.ChangedAt < queryEndDate)
            .OrderBy(h => h.ChangedAt) // Ordenar para la tabla de eventos
            .ToListAsync();
        // --- FIN DE LA CORRECCIÓN ---

        var mildStressAlerts = statusHistory.Count(h => h.Status == PlantStatus.MILD_STRESS);
        var severeStressAlerts = statusHistory.Count(h => h.Status == PlantStatus.SEVERE_STRESS);
        var anomalyAlerts = statusHistory.Count(h => h.Status == PlantStatus.UNKNOWN);

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
            ObservationData = observationData,
            StatusHistory = statusHistory 
        };

        _logger.LogInformation("Generando reporte en PDF para la planta {PlantName}", plant.Name);
        var document = new PlantReportDocument(reportModel);
        byte[] pdfBytes = document.GeneratePdf();

        return pdfBytes;
    }
}