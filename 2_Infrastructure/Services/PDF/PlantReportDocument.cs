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
                x.Span(" de ").FontSize(10);
                x.TotalPages().FontSize(10);
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
                col.Item().Text($"Generado el:").FontSize(9);
                col.Item().Text(_model.GenerationDate).FontSize(9);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(25);
            column.Item().Element(ComposeSummary);

            column.Item().Text("Evolución del Índice de Estrés Hídrico (CWSI)").Style(Styles.Header);
            column.Item().Image(GraphGenerator.CreateCwsiGraph(_model.AnalysisData, 0.3f, 0.5f));

            column.Item().Text("Evolución de Temperaturas").Style(Styles.Header);
            column.Item().Image(GraphGenerator.CreateTemperatureGraph(_model.AnalysisData));

            if (_model.ObservationData.Any())
            {
                column.Item().Element(ComposeObservationsTable);
            }
        });
    }

    private void ComposeSummary(IContainer container)
    {
        container.Grid(grid =>
        {
            grid.VerticalSpacing(5);
            grid.HorizontalSpacing(5);
            grid.Columns(4); // 4 columnas para las métricas

            grid.Item(2).Text("Resumen Ejecutivo").Style(Styles.Header);

            grid.Item(1).Element(c => ComposeMetric(c, "CWSI Promedio", _model.AverageCwsi?.ToString("F2") ?? "N/A"));
            grid.Item(1).Element(c => ComposeMetric(c, "CWSI Máximo", _model.MaxCwsi?.ToString("F2") ?? "N/A"));
            grid.Item(1).Element(c => ComposeMetric(c, "Alertas Leves", _model.MildStressAlerts.ToString()));
            grid.Item(1).Element(c => ComposeMetric(c, "Alertas Críticas", _model.SevereStressAlerts.ToString()));
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
        container.Column(column =>
        {
            column.Item().Text("Bitácora de Observaciones Manuales").Style(Styles.Header);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten1).Padding(5).Text("Fecha y Hora");
                    header.Cell().Background(Colors.Grey.Lighten1).Padding(5).Text("Usuario");
                    header.Cell().Background(Colors.Grey.Lighten1).Padding(5).Text("Descripción");
                });

                foreach (var item in _model.ObservationData)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Timestamp.ToString("dd/MM/yyyy HH:mm"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.UserName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Description);
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
}