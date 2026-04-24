using System;

namespace LibraryApp.Domain.Common;

public abstract class Entity
{
	public Guid Id { get; protected set; }
	public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
	public DateTime? UpdatedAt { get; protected set; }
	public bool IsDeleted { get; protected set; }
}
