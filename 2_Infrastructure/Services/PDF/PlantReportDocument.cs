using ArandanoIRT.Web._0_Domain.Common;
using ArandanoIRT.Web._1_Application.DTOs.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ArandanoIRT.Web._2_Infrastructure.Services.Pdf;

public class PlantReportDocument : IDocument
{
    private readonly PlantReportModel _model;

    public PlantReportDocument(PlantReportModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

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

    private void ComposeContent(IContainer container)
    {
        // --- INICIO DE LA CORRECCIÓN DE MAQUETACIÓN ---
        container.Column(column =>
        {
            // --- Contenido de la PRIMERA PÁGINA ---
            column.Item().PaddingTop(3); 
            
            // 1. Resumen Ejecutivo (los cuadros de métricas)
            column.Item().Element(ComposeSummaryMetrics);
            
            // 2. Gráficos
            if (!_model.AnalysisData.Any())
            {
                column.Item().AlignCenter().Text("No hay datos de análisis para mostrar en los gráficos.");
            }
            else
            {
                column.Item().Column(graphContainer =>
                {
                    graphContainer.Spacing(2); // Reducimos el espacio aquí
                    graphContainer.Item().Text("Evolución del Índice de Estrés Hídrico (CWSI)").Style(Styles.Header);
                    graphContainer.Item().Image(GraphGenerator.CreateCwsiGraph(_model.AnalysisData, 0.3f, 0.5f));
                });
                
                column.Item().Column(graphContainer =>
                {
                    graphContainer.Spacing(10); // Reducimos el espacio aquí
                    graphContainer.Item().Text("Evolución de Temperaturas").Style(Styles.Header);
                    graphContainer.Item().Image(GraphGenerator.CreateTemperatureGraph(_model.AnalysisData));
                });
            }

            // 3. Forzar un Salto de Página
            column.Item().PageBreak();

            // --- Contenido de la SEGUNDA PÁGINA ---
            column.Spacing(20); // Reiniciar el espaciado para la nueva página

            // 4. Diagnóstico
            column.Item().Element(ComposeDiagnosis);

            // 5. Tabla de Eventos
            if (_model.StatusHistory.Any())
            {
                column.Item().Element(ComposeEventsTable);
            }

            // 6. Tabla de Observaciones
            if (_model.ObservationData.Any())
            {
                column.Item().Element(ComposeObservationsTable);
            }
        });
        // --- FIN DE LA CORRECCIÓN DE MAQUETACIÓN ---
    }

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

    private void ComposeDiagnosis(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Resumen Ejecutivo").Style(Styles.Header);
            col.Spacing(10);
            col.Item().Text(GenerateDiagnosisText()).FontSize(10);
        });
    }

    private void ComposeMetric(IContainer container, string title, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(5).Column(column =>
        {
            column.Item().AlignCenter().Text(title).FontSize(9);
            column.Item().AlignCenter().Text(value).Bold().FontSize(14);
        });
    }

    private void ComposeObservationsTable(IContainer container)
    {
        // Aplicamos la misma corrección de columnas aquí para consistencia
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
    
    private void ComposeEventsTable(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Tabla de Eventos Relevantes").Style(Styles.Header);
            column.Item().Table(table =>
            {
                // --- INICIO DE LA CORRECCIÓN DE COLUMNAS ---
                table.ColumnsDefinition(columns =>
                {
                    // Columna de fecha con ancho fijo
                    columns.ConstantColumn(120);
                    // Columna de estado con ancho proporcional
                    columns.RelativeColumn(1);
                    // Columna de descripción con más ancho proporcional
                    columns.RelativeColumn(2.5f);
                });
                // --- FIN DE LA CORRECCIÓN DE COLUMNAS ---

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Fecha y Hora");
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nuevo Estado");
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Origen / Observación");
                });

                foreach (var item in _model.StatusHistory)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ChangedAt.ToColombiaTime().ToString("dd/MM/yyyy HH:mm"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Status.ToString().Replace("_", " "));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Observation ?? "N/A");
                }
            });
        });
    }

    // Clase estática interna para los estilos reutilizables
    private static class Styles
    {
        public static TextStyle Title => TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
        public static TextStyle Subtitle => TextStyle.Default.FontSize(10).FontColor(Colors.Grey.Darken1);
        public static TextStyle Header => TextStyle.Default.FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
    }
    
    private string GenerateDiagnosisText()
    {
        if (_model.MaxCwsi == null) return "No hay suficientes datos para generar un diagnóstico.";
        if (_model.MaxCwsi > 0.5) return "La planta ha experimentado periodos de estrés hídrico crítico. Se recomienda revisar el plan de riego y las condiciones ambientales.";
        if (_model.MaxCwsi > 0.3) return "La planta muestra signos de estrés hídrico incipiente. Se recomienda monitorear de cerca y considerar ajustes en el riego.";
        return "El estado hídrico de la planta se ha mantenido en niveles óptimos durante el periodo evaluado.";
    }
}