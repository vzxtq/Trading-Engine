using FluentValidation;
using TradingEngine.Application.Features.MarketData.Queries;

namespace TradingEngineApi.Validators;

public class GetMarketOrderBookQueryValidator : AbstractValidator<GetMarketOrderBookQuery>
{
    public GetMarketOrderBookQueryValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .Matches("^[A-Z]{2,20}$");

        RuleFor(x => x.Limit)
            .InclusiveBetween(5, 100);
    }
}
