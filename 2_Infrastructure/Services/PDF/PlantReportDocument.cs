using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ArandanoIRT.Web._2_Infrastructure.Services.Pdf;

/// <summary>
/// Representa la definición estructural de un informe de planta, implementando la interfaz IDocument de QuestPDF.
/// Recibe un modelo con todos los datos y se encarga de componer el documento con cabeceras, contenido y pies de página.
/// </summary>
public class PlantReportDocument : IDocument
{
    private readonly PlantReportModel _model;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="PlantReportDocument"/>.
    /// </summary>
    /// <param name="model">El modelo de datos que contiene toda la información a renderizar en el reporte.</param>
    public PlantReportDocument(PlantReportModel model)
    {
        _model = model;
    }

    /// <summary>
    /// Obtiene los metadatos del documento, como el título, autor, etc.
    /// </summary>
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    /// <summary>
    /// Método principal donde se compone la estructura del documento, página por página.
    /// </summary>
    /// <param name="container">El contenedor principal del documento.</param>
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").FontSize(10);
                x.CurrentPageNumber().FontSize(10);
            });
        });
    }

    /// <summary>
    /// Compone la sección de la cabecera de cada página del documento.
    /// </summary>
    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Reporte de Estado Hídrico").Style(Styles.Title);
                column.Item().Text($"Planta: {_model.PlantName}").Style(Styles.Subtitle);
                column.Item().Text($"Cultivo: {_model.CropName}").Style(Styles.Subtitle);
                column.Item().Text($"Periodo: {_model.DateRange}").Style(Styles.Subtitle);
            });
            row.ConstantItem(150).Column(col =>
            {
                col.Item().AlignRight().Text("Generado el:").FontSize(9);
                col.Item().AlignRight().Text(_model.GenerationDate).FontSize(9);
            });
        });
    }

    /// <summary>
    /// Compone el contenido principal del documento, distribuyendo los elementos en diferentes páginas.
    /// </summary>
    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // --- Contenido de la Primera Página ---
            column.Item().PaddingTop(3);
            column.Item().Element(ComposeSummaryMetrics);

            if (!_model.AnalysisData.Any())
            {
                column.Item().AlignCenter().Text("No hay datos de análisis para mostrar en los gráficos.");
            }
            else
            {
                column.Item().Column(graphContainer =>
                {
                    graphContainer.Spacing(2);
                    graphContainer.Item().Text("Evolución del Índice de Estrés Hídrico (CWSI)").Style(Styles.Header);
                    graphContainer.Item().Image(GraphGenerator.CreateCwsiGraph(_model.AnalysisData, _model.CwsiThresholdIncipient, _model.CwsiThresholdCritical));
                });

                column.Item().Column(graphContainer =>
                {
                    graphContainer.Spacing(10);
                    graphContainer.Item().Text("Evolución de Temperaturas").Style(Styles.Header);
                    graphContainer.Item().Image(GraphGenerator.CreateTemperatureGraph(_model.AnalysisData));
                });
            }

            column.Item().PageBreak();

            // --- Contenido de la Segunda Página ---
            column.Spacing(20);
            column.Item().Element(ComposeDiagnosis);

            if (_model.StatusHistory.Any())
            {
                column.Item().Element(ComposeEventsTable);
            }

            if (_model.ObservationData.Any())
            {
                column.Item().Element(ComposeObservationsTable);
            }
        });
    }

    /// <summary>
    /// Compone la cuadrícula de métricas de resumen en la parte superior del informe.
    /// </summary>
    private void ComposeSummaryMetrics(IContainer container)
    {
        container.Grid(grid =>
        {
            grid.Columns(5);
            grid.Item().Element(c => ComposeMetric(c, "CWSI Promedio", _model.AverageCwsi?.ToString("F2") ?? "N/A"));
            grid.Item().Element(c => ComposeMetric(c, "CWSI Máximo", _model.MaxCwsi?.ToString("F2") ?? "N/A"));
            grid.Item().Element(c => ComposeMetric(c, "Alertas Leves", _model.MildStressAlerts.ToString()));
            grid.Item().Element(c => ComposeMetric(c, "Alertas Críticas", _model.SevereStressAlerts.ToString()));
            grid.Item().Element(c => ComposeMetric(c, "Anomalías", _model.AnomalyAlerts.ToString()));
        });
    }

    /// <summary>
    /// Compone la sección de diagnóstico o resumen ejecutivo.
    /// </summary>
    private void ComposeDiagnosis(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Resumen Ejecutivo").Style(Styles.Header);
            col.Spacing(10);
            col.Item().Text(GenerateDiagnosisText()).FontSize(10);
        });
    }

    /// <summary>
    /// Compone un único cuadro de métrica con un título y un valor.
    /// </summary>
    private void ComposeMetric(IContainer container, string title, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(5).Column(column =>
        {
            column.Item().AlignCenter().Text(title).FontSize(9);
            column.Item().AlignCenter().Text(value).Bold().FontSize(14);
        });
    }

    /// <summary>
    /// Compone la tabla que muestra la bitácora de observaciones manuales.
    /// </summary>
    private void ComposeObservationsTable(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Bitácora de Observaciones Manuales").Style(Styles.Header);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Fecha y Hora");
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Usuario");
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Descripción");
                });

                foreach (var item in _model.ObservationData)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Timestamp.ToColombiaTime().ToString("dd/MM/yyyy HH:mm"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.UserName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Description);
                }
            });
        });
    }

    /// <summary>
    /// Compone la tabla que muestra el historial de cambios de estado (eventos relevantes).
    /// </summary>
    private void ComposeEventsTable(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Tabla de Eventos Relevantes").Style(Styles.Header);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Fecha y Hora");
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nuevo Estado");
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Origen / Observación");
                });

                foreach (var item in _model.StatusHistory)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ChangedAt.ToColombiaTime().ToString("dd/MM/yyyy HH:mm"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Status.GetDisplayName());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Observation ?? "N/A");
                }
            });
        });
    }

    /// <summary>
    /// Genera un texto de diagnóstico simple basado en el valor máximo de CWSI del reporte.
    /// </summary>
    private string GenerateDiagnosisText()
    {
        if (_model.MaxCwsi == null) return "No hay suficientes datos para generar un diagnóstico.";
        if (_model.MaxCwsi > _model.CwsiThresholdCritical) return "La planta ha experimentado periodos de estrés hídrico crítico. Se recomienda revisar el plan de riego y las condiciones ambientales.";
        if (_model.MaxCwsi > _model.CwsiThresholdIncipient) return "La planta muestra signos de estrés hídrico incipiente. Se recomienda monitorear de cerca y considerar ajustes en el riego.";
        return "El estado hídrico de la planta se ha mantenido en niveles óptimos durante el periodo evaluado.";
    }

    /// <summary>
    /// Clase estática interna que define los estilos de texto reutilizables para el documento.
    /// </summary>
    private static class Styles
    {
        public static TextStyle Title => TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
        public static TextStyle Subtitle => TextStyle.Default.FontSize(10).FontColor(Colors.Grey.Darken1);
        public static TextStyle Header => TextStyle.Default.FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
    }
}