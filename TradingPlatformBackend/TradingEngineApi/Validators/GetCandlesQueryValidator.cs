using FluentValidation;
using TradingEngine.Application.Features.MarketData.Queries;

namespace TradingEngine.Api.Validators;

public class GetCandlesQueryValidator : AbstractValidator<GetCandlesQuery>
{
    private static readonly string[] AllowedIntervals =
        ["1m", "3m", "5m", "15m", "30m", "1h", "2h", "4h", "6h", "8h", "12h", "1d", "3d", "1w", "1M"]; //TODO consider move it to a shared constant

    public GetCandlesQueryValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .Matches("^[A-Z]{2,20}$");

        RuleFor(x => x.Interval)
            .NotEmpty()
            .Must(i => AllowedIntervals.Contains(i))
            .WithMessage($"Interval must be one of: {string.Join(", ", AllowedIntervals)}");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 1000);
    }
}
