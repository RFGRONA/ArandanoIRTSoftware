using System.ComponentModel.DataAnnotations;
using ArandanoIRT.Web._0_Domain.Entities;

namespace ArandanoIRT.Web._1_Application.Helper;

[AttributeUsage(AttributeTargets.Class)]
public class ValidateAnalysisParametersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        const int minSolarHour = 8;
        const int maxSolarHour = 16;

        // El atributo se aplica a la clase, por lo que 'value' es una instancia de AnalysisParameters.
        if (value is not AnalysisParameters analysisParams)
            // Si no es del tipo esperado, dejamos que otras validaciones se encarguen.
            return ValidationResult.Success;

        // Validación 1: El umbral de estrés incipiente debe ser menor que el crítico.
        if (analysisParams.CwsiThresholdIncipient >= analysisParams.CwsiThresholdCritical)
            return new ValidationResult(
                "El umbral de estrés incipiente debe ser menor que el umbral de estrés crítico.",
                new[]
                {
                    nameof(AnalysisParameters.CwsiThresholdIncipient), nameof(AnalysisParameters.CwsiThresholdCritical)
                }
            );

        // Validación 2: La hora de inicio debe ser menor que la hora de fin.
        if (analysisParams.AnalysisWindowStartHour >= analysisParams.AnalysisWindowEndHour)
            return new ValidationResult(
                "La hora de inicio del análisis debe ser anterior a la hora de fin.",
                new[]
                {
                    nameof(AnalysisParameters.AnalysisWindowStartHour), nameof(AnalysisParameters.AnalysisWindowEndHour)
                }
            );

        // Validación 3: La ventana de análisis debe estar dentro del rango de actividad solar.
        if (analysisParams.AnalysisWindowStartHour < minSolarHour ||
            analysisParams.AnalysisWindowEndHour > maxSolarHour)
            return new ValidationResult(
                $"La ventana de análisis debe estar completamente dentro del rango de alta actividad solar ({minSolarHour}:00 - {maxSolarHour}:00).",
                new[]
                {
                    nameof(AnalysisParameters.AnalysisWindowStartHour), nameof(AnalysisParameters.AnalysisWindowEndHour)
                }
            );

        return ValidationResult.Success;
    }
}