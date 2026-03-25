namespace server_dotnet.Constants;

public static class AppConstants
{
    // JWT
    public const int TokenExpirationDays = 7;

    // Version History
    public const int MaxVersionsToKeep = 30;
    public const int VersionsPageSize = 50;

    // Validation
    public const int MinPasswordLength = 8;
    public const int MaxUsernameLength = 50;
    public const int MaxEmailLength = 100;
    public const int MinUsernameLength = 2;

    // Share Token
    public const int ShareTokenLength = 32;  // bytes

    // Rate Limiting
    public const int AuthRateLimitPermitCount = 5;
    public const int AuthRateLimitWindowMinutes = 1;

    // Auto-save
    public const int AutoSaveDebounceMs = 3000;
    public const int SignificantChangeThreshold = 100;
}
