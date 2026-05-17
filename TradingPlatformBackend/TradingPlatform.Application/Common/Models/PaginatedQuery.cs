namespace TradingEngine.Application.Common.Models;

public abstract record PaginatedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize < 1 ? 10 : PageSize;
}
