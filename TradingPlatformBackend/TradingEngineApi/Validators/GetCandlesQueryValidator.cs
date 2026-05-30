using FluentValidation;
using TradingEngine.Application.Features.MarketData.Queries;
using TradingEngine.Domain.Constants;

namespace TradingEngine.Api.Validators;

public class GetCandlesQueryValidator : AbstractValidator<GetCandlesQuery>
{
    public GetCandlesQueryValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .Matches("^[A-Z]{2,20}$");

        RuleFor(x => x.Interval)
            .NotEmpty()
            .Must(i => Timeframes.SupportedTimeframes.Contains(i))
            .WithMessage($"Interval must be one of: {string.Join(", ", Timeframes.SupportedTimeframes)}");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 1000);
    }
}
