using System;
using System.Threading.Tasks;
using ArandanoIRT.Web._0_Domain.Entities;
using ArandanoIRT.Web._1_Application.Services.Contracts;
using ArandanoIRT.Web._1_Application.Services.Implementation;
using ArandanoIRT.Web._2_Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArandanoIRT.Tests.Services;

public class AnalysisExecutionServiceTests
{
    private readonly Mock<ILogger<AnalysisExecutionService>> _loggerMock = new();
    private readonly Mock<IConditionPredictor> _predictorMock = new();
    private readonly Mock<IDataQueryService> _dataQueryServiceMock = new();
    private readonly Mock<ICropService> _cropServiceMock = new();
    private readonly Mock<IAlertTriggerService> _alertTriggerServiceMock = new();

    private AnalysisExecutionService CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDB_" + Guid.NewGuid())
            .Options;
        var dbContext = new ApplicationDbContext(options);

        // Predecir que siempre es apto para simplificar el test de cálculo
        _predictorMock.Setup(p => p.IsConditionSuitableAsync(It.IsAny<float[]>())).ReturnsAsync(1);

        return new AnalysisExecutionService(
            dbContext,
            _loggerMock.Object,
            _predictorMock.Object,
            _dataQueryServiceMock.Object,
            _cropServiceMock.Object,
            _alertTriggerServiceMock.Object
        );
    }

    [Fact]
    public async Task CalculateCwsiAsync_ValidInput_CalculatesCorrectly()
    {
        // Arrange
        var service = CreateService();

        // RecordedAt = 12pm colombian time. So UTC would be approx 17:00.
        // We will create a DateTime that results in hour=12 after ToColombiaTime().
        var time12pm = new DateTime(2024, 01, 01, 17, 0, 0, DateTimeKind.Utc); // 12:00 PM Colombia time
        var thresholdAt12 = 5f; // From AnalysisExecutionService HourlyStressThresholds

        var reading = new EnvironmentalReading
        {
            Temperature = 25f,
            Humidity = 60f,
            RecordedAtServer = time12pm,
            ExtraData = "{\"light\": 50000}"
        };

        var parameters = new AnalysisParameters 
        { 
            EmpiricalM = -1.56, 
            EmpiricalC = -0.21, 
            EmpiricalUl = 5.0, 
            MinValidCanopyTemp = 5.0, 
            MaxValidCanopyTemp = 45.0 
        };

        // Approximated VPD for 25C and 60% Humidity is ~1.267 kPa
        // LL = (-1.56 * 1.267) - 0.21 = -2.186
        // UL = 5.0
        // T_canopy = 24.0, T_ambient = 25.0 => tDiff = -1.0
        // CWSI = (-1.0 - (-2.186)) / (5.0 - (-2.186)) = 1.186 / 7.186 ≈ 0.165
        
        var monitoredCapture = new ThermalCapture { ThermalDataStats = "{\"Avg_Temp\": 24.0}" };

        var input = new IAnalysisExecutionService.CwsiCalculationInput(
            reading, monitoredCapture, monitoredPlant, parameters
        );

        // Act
        var result = await service.CalculateCwsiAsync(input);

        // Assert
        Assert.True(result.IsSuccess, "Calculation failed: " + result.ErrorMessage);
        Assert.Equal(-2.186, (double)result.Value.BaselineLL.Value, 1);
        Assert.Equal(5.0, (double)result.Value.BaselineUL.Value, 1);
        Assert.Equal(0.165, (double)result.Value.CwsiValue.Value, 2);
    }

    [Fact]
    public async Task CalculateCwsiAsync_HighTemperature_CalculatesSevereCwsiCappedAt1()
    {
        // Arrange
        var service = CreateService();
        var time12pm = new DateTime(2024, 01, 01, 17, 0, 0, DateTimeKind.Utc);

        var reading = new EnvironmentalReading
        {
            Temperature = 28f,
            Humidity = 50f,
            RecordedAtServer = time12pm,
            ExtraData = "{\"light\": 60000}"
        };

        var parameters = new AnalysisParameters 
        { 
            EmpiricalM = -1.56, 
            EmpiricalC = -0.21, 
            EmpiricalUl = 5.0, 
            MinValidCanopyTemp = 5.0, 
            MaxValidCanopyTemp = 45.0 
        };

        // For Temp 28, Hum 50%, VPD is ~1.89 kPa
        // LL = (-1.56 * 1.89) - 0.21 = -3.158
        // UL = 5.0
        // T_canopy = 38.0, T_ambient = 28.0 => tDiff = 10.0
        // CWSI = (10.0 - (-3.158)) / (5.0 - (-3.158)) = 13.158 / 8.158 = 1.6 > 1.0 (Clamped)
        
        var monitoredCapture = new ThermalCapture { ThermalDataStats = "{\"Avg_Temp\": 38.0}" };
        var monitoredPlant = new Plant { Id = 2 };

        var input = new IAnalysisExecutionService.CwsiCalculationInput(
            reading, monitoredCapture, monitoredPlant, parameters
        );

        // Act
        var result = await service.CalculateCwsiAsync(input);

        // Assert
        Assert.True(result.IsSuccess, "Calculation failed: " + result.ErrorMessage);
        Assert.Equal(1.0, (double)result.Value.CwsiValue.Value, 2); // clamped
    }
}
