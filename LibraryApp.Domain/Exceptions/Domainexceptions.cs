namespace LibraryApp.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

// Kayıt bulunamadı → 404
public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} bulunamadı. (Id: {key})") { }
}

// İş kuralı ihlali → 422
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}

// Yetki yok → 403
public class UnauthorizedException : DomainException
{
    public UnauthorizedException() : base("Bu işlem için yetkiniz yok") { }
}