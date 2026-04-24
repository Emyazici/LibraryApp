using MediatR;

namespace LibraryApp.Domain.Common
{
    public interface IDomainEvent : INotification
    {
        Guid Id { get; }
        DateTime OccurredOn { get; }
    }
}