using ArandanoIRT.Web._1_Application.DTOs.Reports;
using SkiaSharp;

namespace ArandanoIRT.Web._2_Infrastructure.Services.Pdf;

/// <summary>
/// Clase de utilidad estática para generar imágenes de gráficos utilizando la librería SkiaSharp.
/// Estos gráficos están diseñados para ser incrustados en los informes PDF.
/// </summary>
public static class GraphGenerator
{
    private const int Width = 800;
    private const int Height = 400;
    private const int Padding = 60;

    /// <summary>
    /// Crea un gráfico de líneas para los datos de CWSI a lo largo del tiempo.
    /// Incluye líneas de umbral para estrés incipiente y crítico, y una leyenda.
    /// </summary>
    /// <param name="data">La lista de puntos de datos de análisis.</param>
    /// <param name="thresholdIncipient">El valor del umbral de estrés incipiente.</param>
    /// <param name="thresholdCritical">El valor del umbral de estrés crítico.</param>
    /// <returns>Un arreglo de bytes que representa la imagen del gráfico en formato PNG.</returns>
    public static byte[] CreateCwsiGraph(List<AnalysisResultDataPoint> data, float thresholdIncipient, float thresholdCritical)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        if (data == null || !data.Any())
        {
            return DrawPlaceholder(surface, "No hay datos de CWSI para mostrar");
        }

        var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, TextSize = 12 };
        var dataPaint = new SKPaint { Color = SKColors.DodgerBlue, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

        // Dibujar Ejes
        DrawAxes(canvas, textPaint, data.First().Timestamp, data.Last().Timestamp, 0f, 1f, "CWSI");

        // --- INICIO DE LA CORRECCIÓN #1: DIBUJAR LÍNEAS SIN TEXTO ---
        // Dibujamos las líneas de umbral sin intentar añadirles el texto al final
        var incipientPaint = new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0), IsAntialias = true };
        var criticalPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0), IsAntialias = true };

        float yIncipient = MapY(thresholdIncipient, 0f, 1f);
        canvas.DrawLine(Padding, yIncipient, Width - Padding, yIncipient, incipientPaint);

        float yCritical = MapY(thresholdCritical, 0f, 1f);
        canvas.DrawLine(Padding, yCritical, Width - Padding, yCritical, criticalPaint);
        // --- FIN DE LA CORRECCIÓN #1 ---

        // Dibujar Datos (sin cambios)
        var path = new SKPath();
        path.MoveTo(MapCoordinates(data[0].Timestamp, data[0].CwsiValue, data.First().Timestamp, data.Last().Timestamp, 0f, 1f));
        foreach (var point in data.Skip(1))
        {
            path.LineTo(MapCoordinates(point.Timestamp, point.CwsiValue, data.First().Timestamp, data.Last().Timestamp, 0f, 1f));
        }
        canvas.DrawPath(path, dataPaint);

        // --- INICIO DE LA CORRECCIÓN #2: DIBUJAR LEYENDA PERSONALIZADA ---
        // Dibujamos los cuadros de leyenda en la parte superior del gráfico
        float legendY = Padding - 45; // Posición vertical de la leyenda
        float legendX = Width - Padding - 250; // Posición horizontal

        // Leyenda para Estrés Incipiente
        canvas.DrawRect(legendX, legendY, 15, 10, incipientPaint);
        canvas.DrawText("Estrés Incipiente", legendX + 20, legendY + 10, textPaint);

        // Leyenda para Estrés Crítico
        legendX += 130; // Mover a la derecha para la siguiente leyenda
        canvas.DrawRect(legendX, legendY, 15, 10, criticalPaint);
        canvas.DrawText("Estrés Crítico", legendX + 20, legendY + 10, textPaint);
        // --- FIN DE LA CORRECCIÓN #2 ---

        return EncodeSurfaceToPng(surface);
    }

    /// <summary>
    /// Crea un gráfico de líneas dual que compara la temperatura de la canopia y la temperatura ambiente a lo largo del tiempo.
    /// </summary>
    /// <param name="data">La lista de puntos de datos de análisis.</param>
    /// <returns>Un arreglo de bytes que representa la imagen del gráfico en formato PNG.</returns>
    public static byte[] CreateTemperatureGraph(List<AnalysisResultDataPoint> data)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        if (data == null || !data.Any())
        {
            return DrawPlaceholder(surface, "No hay datos de Temperatura para mostrar");
        }

        // Configuración de Pinceles
        var axisPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, StrokeWidth = 1 };
        var gridPaint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1, PathEffect = SKPathEffect.CreateDash(new float[] { 2, 2 }, 0) };
        var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, TextSize = 12 };
        var canopyPaint = new SKPaint { Color = SKColors.ForestGreen, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
        var ambientPaint = new SKPaint { Color = SKColors.DarkOrange, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };

        // Escala del Eje Y
        float minY = data.Min(d => Math.Min(d.CanopyTemperature, d.AmbientTemperature)) - 2;
        float maxY = data.Max(d => Math.Max(d.CanopyTemperature, d.AmbientTemperature)) + 2;

        // Dibujar Ejes
        DrawAxes(canvas, textPaint, data.First().Timestamp, data.Last().Timestamp, minY, maxY, "Temperatura (°C)");

        // Dibujar Datos de Canopia
        var canopyPath = new SKPath();
        canopyPath.MoveTo(MapCoordinates(data[0].Timestamp, data[0].CanopyTemperature, data.First().Timestamp, data.Last().Timestamp, minY, maxY));
        foreach (var point in data.Skip(1))
        {
            canopyPath.LineTo(MapCoordinates(point.Timestamp, point.CanopyTemperature, data.First().Timestamp, data.Last().Timestamp, minY, maxY));
        }
        canvas.DrawPath(canopyPath, canopyPaint);

        // Dibujar Datos de Ambiente
        var ambientPath = new SKPath();
        ambientPath.MoveTo(MapCoordinates(data[0].Timestamp, data[0].AmbientTemperature, data.First().Timestamp, data.Last().Timestamp, minY, maxY));
        foreach (var point in data.Skip(1))
        {
            ambientPath.LineTo(MapCoordinates(point.Timestamp, point.AmbientTemperature, data.First().Timestamp, data.Last().Timestamp, minY, maxY));
        }
        canvas.DrawPath(ambientPath, ambientPaint);

        // Leyenda
        float legendY = Padding - 45; // Misma posición vertical que el otro gráfico
        float legendX = Width - Padding - 250; // Posición horizontal inicial

        // Leyenda para T. Canopia
        canvas.DrawRect(legendX, legendY, 15, 10, canopyPaint);
        canvas.DrawText("T. Canopia", legendX + 20, legendY + 10, textPaint);

        // Leyenda para T. Ambiente
        legendX += 110; // Mover a la derecha para la siguiente leyenda
        canvas.DrawRect(legendX, legendY, 15, 10, ambientPaint);
        canvas.DrawText("T. Ambiente", legendX + 20, legendY + 10, textPaint);

        return EncodeSurfaceToPng(surface);
    }

    // MÉTODOS AUXILIARES

    /// <summary>
    /// Dibuja los ejes X e Y del gráfico, incluyendo el título y las etiquetas.
    /// </summary>
    private static void DrawAxes(SKCanvas canvas, SKPaint textPaint, DateTime minX, DateTime maxX, float minY, float maxY, string yAxisTitle)
    {
        var axisPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true, StrokeWidth = 1 };

        // Eje Y y su título
        canvas.DrawLine(Padding, Padding, Padding, Height - Padding, axisPaint);
        canvas.Save();
        canvas.RotateDegrees(-90);
        canvas.DrawText(yAxisTitle, -(Height / 2f), Padding - 40, textPaint);
        canvas.Restore();

        // Eje X
        canvas.DrawLine(Padding, Height - Padding, Width - Padding, Height - Padding, axisPaint);

        // Etiquetas Eje Y
        for (int i = 0; i <= 5; i++)
        {
            float val = minY + (maxY - minY) * (i / 5f);
            float y = MapY(val, minY, maxY);
            canvas.DrawText(val.ToString("F1"), Padding - 35, y + 5, textPaint);
            canvas.DrawLine(Padding - 5, y, Padding, y, axisPaint);
        }

        // Etiquetas Eje X
        long totalSeconds = (long)(maxX - minX).TotalSeconds;
        for (int i = 0; i <= 4; i++)
        {
            var date = minX.AddSeconds(totalSeconds * (i / 4.0));
            float x = MapX(date, minX, maxX);
            canvas.DrawText(date.ToString("dd/MM HH:mm"), x - 25, Height - Padding + 20, textPaint);
            canvas.DrawLine(x, Height - Padding, x, Height - Padding + 5, axisPaint);
        }
    }

    /// <summary>
    /// Dibuja una línea de umbral horizontal punteada a través del gráfico.
    /// </summary>
    private static void DrawThresholdLine(SKCanvas canvas, SKPaint textPaint, float value, string label, SKColor color, float minY, float maxY)
    {
        var linePaint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0),
            IsAntialias = true
        };

        float y = MapY(value, minY, maxY);
        canvas.DrawLine(Padding, y, Width - Padding, y, linePaint);

        using var labelPaint = new SKPaint
        {
            Typeface = textPaint.Typeface ?? SKTypeface.Default,
            TextSize = textPaint.TextSize,
            IsAntialias = true,
            Color = color
        };

        canvas.DrawText(label, Width - Padding + 5, y + 5, labelPaint);
    }

    /// <summary>
    /// Mapea un punto de datos (tiempo, valor) a una coordenada de píxel en el lienzo.
    /// </summary>
    private static SKPoint MapCoordinates(DateTime time, float value, DateTime minX, DateTime maxX, float minY, float maxY)
    {
        return new SKPoint(MapX(time, minX, maxX), MapY(value, minY, maxY));
    }

    /// <summary>
    /// Mapea un valor de tiempo del eje X a una coordenada de píxel horizontal.
    /// </summary>
    private static float MapX(DateTime time, DateTime minX, DateTime maxX)
    {
        long totalSeconds = (long)(maxX - minX).TotalSeconds;
        long elapsedSeconds = (long)(time - minX).TotalSeconds;
        if (totalSeconds == 0) return Padding;
        return Padding + (Width - 2 * Padding) * (elapsedSeconds / (float)totalSeconds);
    }

    /// <summary>
    /// Mapea un valor de datos del eje Y a una coordenada de píxel vertical.
    /// </summary>
    private static float MapY(float value, float minY, float maxY)
    {
        if (maxY - minY == 0) return Height - Padding;
        return (Height - Padding) - (Height - 2 * Padding) * ((value - minY) / (maxY - minY));
    }

    /// <summary>
    /// Dibuja un texto de marcador de posición en el centro del lienzo cuando no hay datos para graficar.
    /// </summary>
    private static byte[] DrawPlaceholder(SKSurface surface, string text)
    {
        var canvas = surface.Canvas;
        var paint = new SKPaint { Color = SKColors.Gray, TextSize = 20, TextAlign = SKTextAlign.Center };
        canvas.DrawText(text, Width / 2f, Height / 2f, paint);
        return EncodeSurfaceToPng(surface);
    }

    /// <summary>
    /// Codifica la superficie del lienzo a un arreglo de bytes en formato PNG.
    /// </summary>
    private static byte[] EncodeSurfaceToPng(SKSurface surface)
    {
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 80);
        return data.ToArray();
    }
}