namespace ArandanoIRT.Web._2_Infrastructure.Settings;

/// <summary>
///     Contiene la configuración para los recordatorios de calibración de dispositivos.
///     Estas propiedades se cargan desde la sección "CalibrationReminder" en appsettings.json.
/// </summary>
public class CalibrationReminderSettings
{
    /// <summary>
    ///     Define el nombre de la sección en el archivo appsettings.json.
    /// </summary>
    public const string SectionName = "CalibrationReminder";

    /// <summary>
    ///     El intervalo en meses con el que se deben enviar los recordatorios de calibración.
    /// </summary>
    public int ReminderIntervalMonths { get; set; } = 3;
}