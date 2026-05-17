using System.ComponentModel.DataAnnotations;

namespace TradingEngine.Application.Options;

public sealed class JwtSettings
{
    public const string Section = "Jwt";

    [Required]
    public required string SecretKey { get; set; }

    [Required]
    public required string Issuer { get; set; }

    [Required]
    public required string Audience { get; set; }

    [Required]
    public int ExpiryMinutes { get; set; }
}
