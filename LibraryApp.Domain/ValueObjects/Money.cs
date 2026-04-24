using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.ValueObjects;
public sealed class Money : ValueObject
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount   = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new BusinessRuleException("Miktar negatif olamaz.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new BusinessRuleException("Para birimi boş olamaz.");

        return new Money(amount, currency.ToUpperInvariant());
    }

    // İş kuralı metodu — state değiştirmiyor, yeni nesne döndürüyor
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new BusinessRuleException("Farklı para birimleri toplanamaz.");

        return new Money(Amount + other.Amount, Currency);
    }

    // ★ Amount VE Currency ikisi birden eşitliği belirliyor
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}