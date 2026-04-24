
namespace LibraryApp.Domain.Common
{
    public abstract class AggregateRoot : Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

    	protected void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
    	protected void SetUpdatedAt(DateTime updatedAt) => UpdatedAt = updatedAt;
    }
}