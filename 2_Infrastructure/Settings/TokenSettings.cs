namespace ArandanoIRT.Web._2_Infrastructure.Settings;

public class TokenSettings
{
    public const string SectionName = "TokenSettings";
    public int AccessTokenDurationMinutes { get; set; } = 60;
    public int RefreshTokenDurationDays { get; set; } = 7;
    public int AccessTokenNearExpiryThresholdMinutes { get; set; } = 5;
    public int ActivationCodeExpirationInDays { get; set; } = 1;
}