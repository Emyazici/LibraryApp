using LibraryApp.Domain.Common;
using LibraryApp.Domain.Exceptions;

namespace LibraryApp.Domain.ValueObjects;

public sealed class ISBN : ValueObject
{
    public string Value { get; }

    private ISBN(string value)
    {
        Value = value;
    }

    // Factory method — dışarıdan new ISBN() diyemezsin
    public static ISBN Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException("ISBN boş olamaz.");

        var cleaned = value.Replace("-", "").Replace(" ", "");

        if (cleaned.Length != 10 && cleaned.Length != 13)
            throw new BusinessRuleException("ISBN 10 veya 13 karakter olmalı.");

        if (!cleaned.All(char.IsDigit))
            throw new BusinessRuleException("ISBN sadece rakam içermeli.");

        return new ISBN(cleaned);
    }

    // ★ Burası kritik — eşitliği Value belirliyor
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}