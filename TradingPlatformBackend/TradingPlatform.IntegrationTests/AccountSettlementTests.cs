using FluentAssertions;
using TradingEngine.Domain.Entities;
using TradingEngine.Domain.Enums;
using TradingEngine.Domain.ValueObjects;

namespace TradingPlatform.IntegrationTests;

public class AccountSettlementTests
{
    [Fact]
    public void CommitReservedFunds_ShouldConserveCashAcrossBuyerAndSeller()
    {
        var buyer = UserAccountDomain.Create(
            "buyer.cash@test.com",
            "Buyer",
            "Cash",
            new Money(100_000m, Currency.USD));

        var seller = UserAccountDomain.Create(
            "seller.cash@test.com",
            "Seller",
            "Cash",
            new Money(25_000m, Currency.USD));

        var tradeNotional = new Money(61_725.308625m, Currency.USD);
        var cashBeforeTrade = buyer.Balance.Amount + seller.Balance.Amount;

        buyer.ReserveFunds(tradeNotional);
        buyer.CommitReservedFunds(tradeNotional);
        seller.Deposit(tradeNotional);

        buyer.Balance.Amount.Should().Be(38_274.691375m);
        buyer.ReservedBalance.Amount.Should().Be(0m);
        seller.Balance.Amount.Should().Be(86_725.308625m);
        (buyer.Balance.Amount + seller.Balance.Amount).Should().Be(cashBeforeTrade);
    }
}
